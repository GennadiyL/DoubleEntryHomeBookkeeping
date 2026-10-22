using Microsoft.EntityFrameworkCore;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Identifies the Local SQLite context in the dependency container.
/// Inherits the complete shared SQLite model and storage conversions.
/// Typed options bind this context to its own configured connection.
/// The matching unit of work and repositories share its scoped instance.
/// The other database role maintains independent tracking and transactions.
/// This class adds no role-specific tables or business rules.
/// Infrastructure owns connection selection and scope creation.
/// Scope disposal closes this context after its work completes.
/// </summary>
public sealed class LocalAppDbContext : SqLiteAppDbContext
{
	public LocalAppDbContext(DbContextOptions<LocalAppDbContext> options) : base(options)
	{
	}
}
