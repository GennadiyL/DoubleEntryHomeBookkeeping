namespace DataAccess.Core.Behaviors;

public interface ICommand
{
	public Task<TOutput> RunAsync<TInput, TOutput>(TInput data);
}
