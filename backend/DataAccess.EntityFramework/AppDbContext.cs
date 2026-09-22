using DataAccess.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.EntityFramework;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	internal DbSet<Account> Accounts { get; set; } = null!;
	internal DbSet<AccountGroup> AccountGroups { get; set; } = null!;
	internal DbSet<Category> Categories { get; set; } = null!;
	internal DbSet<CategoryGroup> CategoryGroups { get; set; } = null!;
	internal DbSet<Correspondent> Correspondents { get; set; } = null!;
	internal DbSet<CorrespondentGroup> CorrespondentGroups { get; set; } = null!;
	internal DbSet<Currency> Currencies { get; set; } = null!;
	internal DbSet<CurrencyRate> CurrencyRates { get; set; } = null!;
	internal DbSet<Project> Projects { get; set; } = null!;
	internal DbSet<ProjectGroup> ProjectGroups { get; set; } = null!;
	internal DbSet<Template> Templates { get; set; } = null!;
	internal DbSet<TemplateEntry> TemplateEntries { get; set; } = null!;
	internal DbSet<TemplateGroup> TemplateGroups { get; set; } = null!;
	internal DbSet<Transaction> Transactions { get; set; } = null!;
	internal DbSet<TransactionEntry> TransactionEntries { get; set; } = null!;
	internal DbSet<SystemConfig> SystemConfigs { get; set; } = null!;
	internal DbSet<UserConfig> UserConfigs { get; set; } = null!;
}
