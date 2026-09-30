using Business.Models.Entities.Config;
using Business.Models.Enums;
using NUnit.Framework;

namespace UnitTests.Business.Models;

[TestFixture(Category = "Local")]
public sealed class ConfigurationTests
{
	[Test]
	public void SystemConfig_Defaults_MatchPrecisionAndTrackingContract()
	{
		SystemConfig config = new();
		Assert.Multiple(() =>
		{
			Assert.That(config.AmountPrecision, Is.EqualTo(2));
			Assert.That(config.RatePrecision, Is.EqualTo(4));
			Assert.That(config.BalancingAccountId, Is.Null);
			Assert.That(config.BalancingAccount, Is.Null);
			Assert.That(config.EditRevision, Is.Null);
			Assert.That(config.DeleteRevision, Is.Null);
			Assert.That(config.ModificationType, Is.EqualTo(ModificationType.None));
		});
	}

	[Test]
	public void UserConfig_Defaults_MatchNameFormatContract()
	{
		LocalConfig config = new();
		Assert.That(config.DefaultAccountNameOrder, Is.EqualTo(DefaultAccountNameOrder.CorrespondentCategoryProject));
		Assert.That(config.DefaultAccountNameSeparator, Is.EqualTo("/"));
		Assert.That(Enum.GetValues<DefaultAccountNameOrder>(), Has.Length.EqualTo(6));
	}
}
