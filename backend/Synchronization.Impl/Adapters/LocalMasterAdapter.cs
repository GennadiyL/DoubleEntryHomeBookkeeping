using Business.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Master.Contracts.Services;
using Synchronization.Contracts.Adapters;
using Synchronization.Contracts.Adapters.Master;
using MasterDtos = Master.Contracts.Services.Synchronizations;

namespace Synchronization.Impl.Adapters;

/// <summary>
/// Maps the local synchronization boundary to the in-process Master service.
/// Each operation resolves Master in a fresh scope because its database session is disposed after use.
/// Adapter and service DTOs remain separate contract types.
/// The shared exchange container passes through without changing batch identities.
/// Cancellation and service failures propagate to the caller.
/// Snapshot streams pass through without buffering or disposal.
/// The caller retains ownership of the returned snapshot stream.
/// The host establishes caller authentication and dataset authorization.
/// </summary>
internal sealed class LocalMasterAdapter : IMasterAdapter
{
	private readonly IServiceScopeFactory _scopeFactory;

	public LocalMasterAdapter(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

	public Task<AuthorizationResult> CheckAuthorization(
		string masterDatasetKey,
		string localDatasetKey,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(AuthorizationResult.Authorized);
	}
	public async Task<SynchronizeResponse> Synchronize(SynchronizeRequest request, CancellationToken cancellationToken = default)
	{
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		MasterDtos.SynchronizeResponse result = await scope.ServiceProvider.GetRequiredService<IMasterService>().Synchronize(new MasterDtos.SynchronizeRequest
		{
			Container = request.Container,
		}, cancellationToken).ConfigureAwait(false);
		return new SynchronizeResponse { Revision = result.Revision };
	}

	public async Task<DownloadResponse> Download(DownloadRequest request, CancellationToken cancellationToken = default)
	{
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		MasterDtos.DownloadResponse result = await scope.ServiceProvider.GetRequiredService<IMasterService>().Download(new MasterDtos.DownloadRequest
		{
			MasterDatasetKey = request.MasterDatasetKey,
			LocalDatasetKey = request.LocalDatasetKey,
			MinimumRevision = request.MinimumRevision,
		}, cancellationToken).ConfigureAwait(false);
		return new DownloadResponse { Revision = result.Revision, Content = result.Content };
	}

	public async Task<AcknowledgeResponse> Acknowledge(AcknowledgeRequest request, CancellationToken cancellationToken = default)
	{
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		MasterDtos.AcknowledgeResponse result = await scope.ServiceProvider.GetRequiredService<IMasterService>().Acknowledge(new MasterDtos.AcknowledgeRequest
		{
			MasterDatasetKey = request.MasterDatasetKey,
			LocalDatasetKey = request.LocalDatasetKey,
			InstalledRevision = request.InstalledRevision,
		}, cancellationToken).ConfigureAwait(false);
		return new AcknowledgeResponse { IsSuccess = result.IsSuccess };
	}
}
