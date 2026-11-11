namespace DataAccess.Contracts.Commands;

public interface IValidateCandidateCommand
{
	/// <summary>
	/// Checks the downloaded Candidate database integrity and compatibility before preparation.
	/// </summary>
	public Task Validate(CancellationToken cancellationToken = default);
}
