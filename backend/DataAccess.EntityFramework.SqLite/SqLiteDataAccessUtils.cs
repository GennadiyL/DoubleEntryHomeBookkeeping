using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Defines the SqLite database lifecycle utilities.
/// Provides host-level helpers for creating or migrating a SqLite database.
/// A host invokes the helpers from a root service provider during startup.
/// Each helper creates a scope and resolves the configured AppDbContext.
/// Business and repository code do not depend on these startup utilities.
/// </summary>
public static class SqLiteDataAccessUtils
{
	public static async Task EnsureCreatedAsync(this IServiceProvider serviceProvider)
	{
		using IServiceScope scope = serviceProvider.CreateScope();
		AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.EnsureCreatedAsync();
	}

	public static async Task MigrateAsync(this IServiceProvider serviceProvider)
	{
		using IServiceScope scope = serviceProvider.CreateScope();
		AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync();
	}
}
