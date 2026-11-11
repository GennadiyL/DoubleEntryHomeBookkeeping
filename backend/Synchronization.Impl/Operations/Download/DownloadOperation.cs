using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.Core.Entities;
using Microsoft.Extensions.Configuration;
using Shared.Contracts;
using Synchronization.Contracts.Adapters;
using Synchronization.Contracts.Adapters.Master;
using Exchange.Models.Synchronizations;

namespace Synchronization.Impl.Operations.Download;

/// <summary>
/// Downloads and prepares a consistent Master snapshot outside the active database.
/// Validates dataset identity, immutable settings and the actual snapshot revision.
/// Preserves Local configuration and the saved synchronization container in the candidate.
/// Rebuilds cumulative amounts before activation.
/// Confirmed batches are removed and the installed revision is written into the candidate.
/// Atomic file replacement installs prepared data and recovery state together.
/// Cancellation before activation leaves the existing Local database intact.
/// Temporary files are owned by this operation and cleaned after connections close.
/// </summary>
internal sealed class DownloadOperation : IDownloadOperation
{
	private readonly ISharedContext _shared;
	private readonly IUnitOfWorkFactory _factory;
	private readonly IMasterAdapter _master;
	private readonly IPrepareCandidateCommand _prepareCandidate;
	private readonly IValidateCandidateCommand _validateCandidate;
	private readonly IInstallCandidateCommand _installCandidate;
	private readonly ICleanDataFilesCommand _cleanDataFiles;
	private readonly string _candidatePath;

	public DownloadOperation(
		ISharedContext shared,
		IUnitOfWorkFactory factory,
		IMasterAdapter master,
		IPrepareCandidateCommand prepareCandidate,
		IValidateCandidateCommand validateCandidate,
		IInstallCandidateCommand installCandidate,
		IConfiguration configuration,
		ICleanDataFilesCommand cleanDataFiles)
	{
		_cleanDataFiles = cleanDataFiles;
		_shared = shared;
		_factory = factory;
		_master = master;
		_prepareCandidate = prepareCandidate;
		_validateCandidate = validateCandidate;
		_installCandidate = installCandidate;
		_candidatePath = Path.Combine(
			Path.GetFullPath(configuration[StorageValues.LocalDirectoryConfigurationKey]
				?? throw new InvalidOperationException("Configuration 'DataAccess:LocalDirectory' is required.")),
			StorageValues.CandidateFileName);
	}

	public async Task<long> Run(
		DownloadRequest command,
		SyncContainer pending,
		CancellationToken cancellationToken = default)
	{
		DownloadResponse response = await _master.Download(command, cancellationToken);
		await using Stream content = response.Content;
		if (response.Revision < command.MinimumRevision)
		{
			throw new InvalidSyncBatchException("Downloaded revision is below the required minimum.");
		}
		string path = _candidatePath;
		await _factory.CloseCandidate();
		_cleanDataFiles.CleanAll(_candidatePath);
		try
		{
			await using (Stream destination = _shared.FileService.Open(path, new FileStreamOptions
			{
				Mode = FileMode.CreateNew,
				Access = FileAccess.Write,
				Share = FileShare.None,
				Options = FileOptions.Asynchronous
			}))
			{
				await content.CopyToAsync(destination, cancellationToken);
				await destination.FlushAsync(cancellationToken);
			}
			await _validateCandidate.Validate(cancellationToken);
			await Prepare(
				command,
				response.Revision,
				pending,
				cancellationToken);
			await _installCandidate.Install(cancellationToken);
			return response.Revision;
		}
		finally
		{
			await _factory.CloseCandidate();
			_cleanDataFiles.CleanAll(_candidatePath);
		}
	}

	private async Task Prepare(
		DownloadRequest command,
		long revision,
		SyncContainer pending,
		CancellationToken token)
	{
		ILocalUnitOfWork localUnit = _factory.CreateLocal();
		LocalConfig local = (await localUnit.LocalConfigRepo.GetAll(token)).Single();
		SystemConfig current = (await localUnit.SystemConfigRepo.GetAll(token)).Single();
		ICandidateUnitOfWork candidate = _factory.CreateCandidate();
		SystemConfig downloaded = (await candidate.SystemConfigRepo.GetAll(token)).Single();
		if (downloaded.MasterDatasetKey != command.MasterDatasetKey ||
			downloaded.CurrentRevision != revision ||
			downloaded.BaseCurrencyId != current.BaseCurrencyId ||
			downloaded.AmountPrecision != current.AmountPrecision ||
			downloaded.RatePrecision != current.RatePrecision ||
			local.LocalDatasetKey != command.LocalDatasetKey ||
			await candidate.LocalRegistrationRepo.GetByLocalDatasetKey(command.LocalDatasetKey, token) is null)
		{
			throw new InvalidSyncBatchException("Downloaded snapshot identity, settings or revision do not match the Local dataset.");
		}
		LocalRegistration registration = (await candidate.LocalRegistrationRepo.GetByLocalDatasetKey(command.LocalDatasetKey, token))!;
		SyncContainer installed = new()
		{
			FormatVersion = pending.FormatVersion, SchemaVersion = pending.SchemaVersion,
			MasterDatasetKey = pending.MasterDatasetKey, LocalDatasetKey = pending.LocalDatasetKey, Revision = revision
		};
		installed.Batches.AddRange(pending.Batches);
		foreach (SyncBatch batch in pending.Batches)
		{
			LocalSyncBatch? receipt = await candidate.LocalSyncBatchRepo.GetBySyncKey(registration.Id, batch.SyncKey, token);
			if (receipt is null || receipt.Revision > revision)
			{
				throw new InvalidSyncBatchException("Downloaded snapshot does not confirm every submitted batch.");
			}
			installed.Batches.Remove(batch);
		}
		local.SnapshotRevision = revision;
		await using IUnitOfWorkTransaction transaction = await candidate.BeginTransaction(token);
		try
		{
			await _prepareCandidate.Prepare(
				local,
				new LocalJson { Json = _shared.JsonService.Serialize(installed) },
				token);
			token.ThrowIfCancellationRequested();
			await candidate.CommitTransaction(transaction, CancellationToken.None);
		}
		catch
		{
			await candidate.RollbackTransaction(transaction, CancellationToken.None);
			throw;
		}
	}
}
