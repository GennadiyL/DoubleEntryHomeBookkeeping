using Microsoft.EntityFrameworkCore;

namespace DataAccess.EntityFramework;

/// <summary>
/// Defines the application database context.
/// Defines the Entity Framework session and entity sets used by application persistence.
/// The dependency injection container creates scoped contexts from provider-specific options.
/// Repositories and units of work share this context to coordinate persistence.
/// It does not expose business workflows or provider-specific connection setup.
/// </summary>
public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
	}
}
