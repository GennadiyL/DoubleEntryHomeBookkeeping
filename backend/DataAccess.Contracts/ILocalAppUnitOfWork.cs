namespace DataAccess.Contracts;

/// <summary>
/// Identifies the Local database unit of work within the application container.
/// Inherits the common repository and transaction contract.
/// Consumers select this interface when they require Local persistence.
/// Its repositories must use the same context as its transaction operations.
/// Implementations retain scoped lifetime and lazy repository resolution.
/// The contract exposes business models without EF Core dependencies.
/// It does not select database files or create dependency scopes.
/// Infrastructure registers the matching provider implementation.
/// </summary>
public interface ILocalAppUnitOfWork : IAppUnitOfWork
{
}
