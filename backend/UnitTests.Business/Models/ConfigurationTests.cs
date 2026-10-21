using Business.Models.Entities.Interfaces;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using NUnit.Framework;

namespace UnitTests.Business.Models;

[TestFixture(Category = "Local")]
public sealed class ConfigurationTests
{
	[Test]
	public void SystemConfig_Defaults_MatchMasterOwnedConfiguration()
	{
		SystemConfig config = new();
		Assert.Multiple(() =>
		{
			Assert.That(config.AmountPrecision, Is.EqualTo(2));
			Assert.That(config.RatePrecision, Is.EqualTo(4));
			Assert.That(config.CurrentRevision, Is.Zero);
			Assert.That(typeof(ITrackedEntity).IsAssignableFrom(typeof(SystemConfig)), Is.False);
		});
	}

	[Test]
	public void UserConfig_Defaults_MatchNameFormatContract()
	{
		LocalConfig config = new();
		Assert.That(config.AccountNameOrder, Is.EqualTo(AccountNameOrder.CorrespondentCategoryProject));
		Assert.That(config.AccountNameSeparator, Is.EqualTo("/"));
		Assert.That(config.AccountNameAddCurrency, Is.False);
		Assert.That((int)AccountNameOrder.Undefined, Is.Zero);
		Assert.That(Enum.GetValues<AccountNameOrder>().Count(value => value != AccountNameOrder.Undefined), Is.EqualTo(6));
	}
}
