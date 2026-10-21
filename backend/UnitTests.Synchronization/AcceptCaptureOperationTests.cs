using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Synchronization.Impl;
using Synchronization.Impl.Operations.AcceptCapture;

namespace UnitTests.Synchronization;

/// <summary>
/// Verifies bulk acceptance through the registered internal operation.
/// Substitute repositories observe ordering without accessing a database.
/// All tracked repositories participate in deletion and tracking acceptance.
/// Referencing rows are deleted before their referenced catalog parents.
/// No acceptance runs before the full deletion phase succeeds.
/// Failures and cancellation propagate instead of implying completion.
/// Transaction ownership and durable batch persistence remain with the caller.
/// Local configuration and entry repositories are not bulk-accepted directly.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class AcceptCaptureOperationTests
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IAppUnitOfWork _unit = null!;
	private IAcceptCaptureOperation _operation = null!;
	private readonly List<string> _calls = new();
	private string? _failure;
	private CancellationToken _expectedToken;
	private static readonly string[] Repositories =
	[
		"SystemConfig",
		"Transaction",
		"Template",
		"Report",
		"Account",
		"CurrencyRate",
		"Category",
		"Correspondent",
		"Project",
		"AccountGroup",
		"TemplateGroup",
		"ReportGroup",
		"CategoryGroup",
		"CorrespondentGroup",
		"ProjectGroup",
		"Currency",
	];

	[SetUp]
	public void SetUp()
	{
		_calls.Clear();
		_failure = null;
		_expectedToken = default;
		_unit = Substitute.For<IAppUnitOfWork>();
		Observe(_unit.SystemConfigRepo, "SystemConfig");
		Observe(_unit.TransactionRepo, "Transaction");
		Observe(_unit.TemplateRepo, "Template");
		Observe(_unit.ReportRepo, "Report");
		Observe(_unit.AccountRepo, "Account");
		Observe(_unit.CurrencyRateRepo, "CurrencyRate");
		Observe(_unit.CategoryRepo, "Category");
		Observe(_unit.CorrespondentRepo, "Correspondent");
		Observe(_unit.ProjectRepo, "Project");
		Observe(_unit.AccountGroupRepo, "AccountGroup");
		Observe(_unit.TemplateGroupRepo, "TemplateGroup");
		Observe(_unit.ReportGroupRepo, "ReportGroup");
		Observe(_unit.CategoryGroupRepo, "CategoryGroup");
		Observe(_unit.CorrespondentGroupRepo, "CorrespondentGroup");
		Observe(_unit.ProjectGroupRepo, "ProjectGroup");
		Observe(_unit.CurrencyRepo, "Currency");
		ServiceCollection services = new();
		services.AddSynchronizationModule();
		services.AddScoped<IAppUnitOfWork>(_ => _unit);
		_provider = services.BuildServiceProvider();
		_scope = _provider.CreateScope();
		_operation = _scope.ServiceProvider.GetRequiredService<IAcceptCaptureOperation>();
	}

	[TearDown]
	public void TearDown()
	{
		_scope.Dispose();
		_provider.Dispose();
	}

	[Test]
	public async Task Run_DeletesBeforeAcceptanceAndPreservesCallerTransactionOwnership()
	{
		using CancellationTokenSource cancellation = new();
		_expectedToken = cancellation.Token;
		await _operation.Run(_expectedToken);
		Assert.That(_calls, Is.EqualTo(Repositories.Select(name => name + ".HardDelete")
			.Concat(Repositories.Select(name => name + ".AcceptChanges"))));
		Assert.That(_unit.ReceivedCalls().All(call => call.GetMethodInfo().Name.StartsWith("get_", StringComparison.Ordinal)), Is.True);
		Assert.That(_unit.LocalConfigRepo.ReceivedCalls(), Is.Empty);
		Assert.That(_unit.TransactionEntryRepo.ReceivedCalls(), Is.Empty);
		Assert.That(_unit.TemplateEntryRepo.ReceivedCalls(), Is.Empty);
	}

	[TestCase("Account.HardDelete")]
	[TestCase("Account.AcceptChanges")]
	public void Run_StopsImmediatelyOnRepositoryFailure(string failure)
	{
		_failure = failure;
		InvalidOperationException? exception = Assert.ThrowsAsync<InvalidOperationException>(() => _operation.Run());
		Assert.That(exception!.Message, Is.EqualTo(failure));
		List<string> allCalls = Repositories.Select(name => name + ".HardDelete")
			.Concat(Repositories.Select(name => name + ".AcceptChanges")).ToList();
		Assert.That(_calls, Is.EqualTo(allCalls.Take(allCalls.IndexOf(failure) + 1)));
		Assert.That(_unit.ReceivedCalls().All(call => call.GetMethodInfo().Name.StartsWith("get_", StringComparison.Ordinal)), Is.True);
	}

	[Test]
	public void Run_CancellationBeforeStartDoesNotMutate()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		Assert.ThrowsAsync<OperationCanceledException>(() => _operation.Run(cancellation.Token));
		Assert.That(_calls, Is.Empty);
	}

	private void Observe<T>(ITrackedRepository<T> repository, string name) where T : class, IBaseEntity, ITrackedEntity
	{
		repository.HardDelete(Arg.Any<CancellationToken>()).Returns(call => Record(name + ".HardDelete", call.Arg<CancellationToken>()));
		repository.AcceptChanges(Arg.Any<CancellationToken>()).Returns(call => Record(name + ".AcceptChanges", call.Arg<CancellationToken>()));
	}

	private Task<int> Record(string call, CancellationToken cancellationToken)
	{
		Assert.That(cancellationToken, Is.EqualTo(_expectedToken));
		_calls.Add(call);
		return call == _failure
			? Task.FromException<int>(new InvalidOperationException(call))
			: Task.FromResult(1);
	}
}