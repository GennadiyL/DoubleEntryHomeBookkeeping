namespace DataAccess.Core.Behaviors;

public interface IQuery
{
	public Task<int> RunAsync<TInput, TOutput>(TInput data, CancellationToken cancellationToken = default);
}
