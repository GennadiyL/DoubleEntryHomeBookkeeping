using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework.Contracts;
using DataAccess.EntityFramework.SqLite.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccess.EntityFramework.SqLite.Commands;

/// <summary>
/// Prepares the downloaded Candidate database through its factory-owned context.
/// Runs after the caller validates the downloaded database.
/// Replaces Local-only configuration and synchronization recovery JSON.
/// Rebuilds account balances in the same Candidate transaction.
/// Repository writes and SQL use the same Candidate connection.
/// The caller decides which pending batches are confirmed.
/// This command neither installs files nor closes units of work.
/// Failure leaves activation to the caller's rollback and cleanup.
/// </summary>
internal sealed class PrepareCandidateCommand : IPrepareCandidateCommand
{
	private readonly IUnitOfWorkFactory _factory;

	public PrepareCandidateCommand(IUnitOfWorkFactory factory) => _factory = factory;

	public async Task Prepare(
		LocalConfig configuration,
		LocalJson pending,
		CancellationToken cancellationToken = default)
	{
		ICandidateUnitOfWork candidate = _factory.CreateCandidate();
		IEntityFrameworkUnitOfWork unit = (IEntityFrameworkUnitOfWork)candidate;
		DbContext context = unit.Database.GetService<ICurrentDbContext>().Context;
		if (unit.Database.CurrentTransaction is null)
		{
			throw new InvalidOperationException("Candidate preparation requires a transaction.");
		}
		await context.Set<DataAccess.EntityFramework.Models.LocalConfig>().ExecuteDeleteAsync(cancellationToken);
		await context.Set<DataAccess.EntityFramework.Models.LocalJson>().ExecuteDeleteAsync(cancellationToken);
		candidate.LocalConfigRepo.Add(configuration);
		candidate.LocalJsonRepo.Add(pending);
		await candidate.SaveChanges(cancellationToken);
		foreach (Business.Models.Entities.Account account in await candidate.AccountRepo.GetAll(cancellationToken))
		{
			await CumulativeAmountSql.Execute(
				unit.Database,
				account.Id,
				null,
				0m,
				cancellationToken);
		}
	}
}
