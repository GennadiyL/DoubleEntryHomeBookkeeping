namespace DataAccess.Contracts.Commands;

public interface IInstallCandidateCommand
{
	/// <summary>
	/// Completes SQLite journals, closes Local and Candidate units, and atomically replaces local.db.
	/// The caller must exclude concurrent Local operations during installation.
	/// </summary>
	public Task Install(CancellationToken cancellationToken = default);
}
