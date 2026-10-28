using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
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
/// SQLite activation installs data and recovery state as one database operation.
/// Cancellation before activation leaves the existing Local database intact.
/// Temporary files are owned by this operation and cleaned after connections close.
/// </summary>
internal sealed class DownloadOperation : IDownloadOperation
{
	private readonly ISharedContext _shared;
	private readonly ILocalAppUnitOfWork _unit;
	private readonly IMasterAdapter _master;
	private readonly IServiceScopeFactory _scopes;

	public DownloadOperation(ISharedContext shared, ILocalAppUnitOfWork unit, IMasterAdapter master, IServiceScopeFactory scopes)
	{
		_shared = shared;
		_unit = unit;
		_master = master;
		_scopes = scopes;
	}

	public async Task<long> Run(DownloadRequest command, SyncContainer pending, CancellationToken cancellationToken = default)
	{
		DownloadResponse response = await _master.Download(command, cancellationToken);
		await using Stream content = response.Content;
		if (response.Revision < command.MinimumRevision)
		{
			throw new InvalidSyncBatchException("Downloaded revision is below the required minimum.");
		}
		string path = Path.Combine(Path.GetTempPath(), $"dehb-install-{Guid.NewGuid():N}.db");
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
			await Prepare(path, command, response.Revision, pending, cancellationToken);
			await _unit.InstallSnapshot(path, cancellationToken);
			return response.Revision;
		}
		finally
		{
			foreach (string suffix in new[] { "", "-wal", "-shm", "-journal" })
			{
				try
				{
					if (_shared.FileService.Exists(path + suffix))
					{
						_shared.FileService.Delete(path + suffix);
					}
				}
				catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
				{
					_shared.LogService.Warn("Could not remove an owned synchronization temporary file.", exception);
				}
			}
		}
	}

	private async Task Prepare(string path, DownloadRequest command, long revision, SyncContainer pending, CancellationToken token)
	{
		LocalConfig local = (await _unit.LocalConfigRepo.GetAll(token)).Single();
		SystemConfig current = (await _unit.SystemConfigRepo.GetAll(token)).Single();
		await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
		ILocalAppUnitOfWork candidate = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		await candidate.OpenSnapshot(path, token);
		SystemConfig downloaded = (await candidate.SystemConfigRepo.GetAll(token)).Single();
		if (downloaded.MasterDatasetKey != command.MasterDatasetKey || downloaded.CurrentRevision != revision ||
			downloaded.BaseCurrencyId != current.BaseCurrencyId || downloaded.AmountPrecision != current.AmountPrecision ||
			downloaded.RatePrecision != current.RatePrecision || local.LocalDatasetKey != command.LocalDatasetKey ||
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
			await candidate.ReplaceLocalState(local, new LocalJson { Json = _shared.JsonService.Serialize(installed) }, token);
			ICumulativeAmountCommand cumulative = scope.ServiceProvider.GetRequiredKeyedService<ICumulativeAmountCommand>(typeof(ILocalAppUnitOfWork));
			foreach (Account account in await candidate.AccountRepo.GetAll(token)) { await cumulative.Recalculate(account.Id, token); }
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
