namespace DataAccess.Core.Behaviors;

public interface IQuery
{
	public Task<int> Run<TInput, TOutput>(TInput data, CancellationToken cancellationToken = default);
}
