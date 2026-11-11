using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;

namespace DataAccess.Contracts.Commands;

public interface IPrepareCandidateCommand
{
	/// <summary>
	/// Replaces Candidate Local configuration and pending JSON and rebuilds cumulative amounts.
	/// The caller owns the Candidate transaction.
	/// </summary>
	public Task Prepare(
		LocalConfig configuration,
		LocalJson pending,
		CancellationToken cancellationToken = default);
}
