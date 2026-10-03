namespace DataAccess.Core.Behaviors;

public interface ICommand
{
	public Task<TOutput> Run<TInput, TOutput>(TInput data, CancellationToken cancellationToken = default);
}
