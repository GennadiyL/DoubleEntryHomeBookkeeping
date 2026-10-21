using Microsoft.Extensions.DependencyInjection;
using Synchronization.Impl.Operations.AcceptCapture;
using Synchronization.Impl.Operations.CaptureChanges;

namespace Synchronization.Impl;

/// <summary>
/// Provides the Synchronization module's dependency injection entry point.
/// Registers the internal capture and acceptance operation implementations.
/// Each operation has a scoped lifetime for the local workflow scope.
/// Infrastructure hosts select this module when composing Local execution.
/// Registration exposes interfaces while retaining internal implementations.
/// This module does not execute capture or acceptance during registration.
/// Capture reads pending changes; acceptance remains a separate implementation placeholder.
/// Master services and provider adapters are composed separately.
/// </summary>
public static class SynchronizationDiConfiguration
{
	public static void AddSynchronizationModule(this IServiceCollection services)
	{
		services.AddScoped<ICaptureChangesOperation, CaptureChangesOperation>();
		services.AddScoped<IAcceptCaptureOperation, AcceptCaptureOperation>();
	}
}
