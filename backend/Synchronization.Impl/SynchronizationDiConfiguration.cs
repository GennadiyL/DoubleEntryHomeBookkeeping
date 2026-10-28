using Synchronization.Contracts.Services;
using Synchronization.Impl.Services;
using Synchronization.Contracts.Adapters;
using Synchronization.Impl.Adapters;
using Synchronization.Impl.Operations.Acknowledge;
using Synchronization.Impl.Operations.Download;
using Microsoft.Extensions.DependencyInjection;
using Synchronization.Impl.Operations.AcceptCapture;
using Synchronization.Impl.Operations.CaptureChanges;

namespace Synchronization.Impl;

/// <summary>
/// Provides the Synchronization module's dependency injection entry point.
/// Registers internal capture, acceptance, download and acknowledgement operations.
/// Each operation has a scoped lifetime for the local workflow scope.
/// Infrastructure hosts select this module when composing Local execution.
/// Registration exposes interfaces while retaining internal implementations.
/// This module does not execute capture or acceptance during registration.
/// Capture reads pending changes; acceptance applies bulk cleanup and tracking updates.
/// Master services and provider adapters are composed separately.
/// </summary>
public static class SynchronizationDiConfiguration
{
	public static void AddLocalMasterAdapter(this IServiceCollection services) => services.AddScoped<IMasterAdapter, LocalMasterAdapter>();

	public static void AddSynchronizationModule(this IServiceCollection services)
	{
		services.AddSingleton<SynchronizationGate>();
		services.AddScoped<ISynchronizationService, SynchronizationService>();
		services.AddScoped<ICaptureChangesOperation, CaptureChangesOperation>();
		services.AddScoped<IAcceptCaptureOperation, AcceptCaptureOperation>();
		services.AddScoped<IDownloadOperation, DownloadOperation>();
		services.AddScoped<IAcknowledgeOperation, AcknowledgeOperation>();
	}
}
