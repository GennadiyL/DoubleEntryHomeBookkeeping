using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Synchronization.Contracts.Adapters;

namespace Adapter.TokenStore.WinUi;

/// <summary>
/// Registers the Windows token-store adapter for the executable host.
/// Token storage belongs to the current Windows user's local application profile.
/// Persisted tokens remain separate from business databases and UI settings.
/// The token store consumes the shared context within its dependency-injection scope.
/// Provider adapters consume its contract without referencing the WinUI host.
/// This module does not select or configure the Master transport implementation.
/// Registration performs no file access or token decryption.
/// Synchronization orchestration remains in the Synchronization domain.
/// </summary>
public static class TokenStoreWinUiDiConfiguration
{
	public static void AddAdapterTokenStoreWinUiModule(this IServiceCollection services)
	{
		services.AddScoped<ITokenStoreAdapter>(provider => new TokenStoreAdapter(provider.GetRequiredService<ISharedContext>(), Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DoubleEntryHomeBookkeeping", "Tokens")));
	}
}
