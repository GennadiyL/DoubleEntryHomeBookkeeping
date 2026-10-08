using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.SqLite.Utils;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Applies SQLite-specific transaction storage mappings.
/// Entry amounts and cumulative amounts use scaled integers.
/// Transaction identities use parameterless Guid.ToByteArray bytes.
/// Entry foreign keys use exactly the same identity representation.
/// Database ordering therefore matches cumulative tie-breaking.
/// Account and entry identities retain their existing representations.
/// Indexes support account membership and transaction-date lookups.
/// Existing databases require explicit schema migration before use.
/// </summary>
public sealed class SqLiteAppDbContext : AppDbContext
{
	public SqLiteAppDbContext(DbContextOptions<SqLiteAppDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<Account>().ToTable(nameof(Account));
		modelBuilder.Entity<AccountGroup>().ToTable(nameof(AccountGroup));
		modelBuilder.Entity<Category>().ToTable(nameof(Category));
		modelBuilder.Entity<CategoryGroup>().ToTable(nameof(CategoryGroup));
		modelBuilder.Entity<Correspondent>().ToTable(nameof(Correspondent));
		modelBuilder.Entity<CorrespondentGroup>().ToTable(nameof(CorrespondentGroup));
		modelBuilder.Entity<Currency>().ToTable(nameof(Currency));
		modelBuilder.Entity<CurrencyRate>().ToTable(nameof(CurrencyRate));
		modelBuilder.Entity<Project>().ToTable(nameof(Project));
		modelBuilder.Entity<Report>().ToTable(nameof(Report));
		modelBuilder.Entity<ReportGroup>().ToTable(nameof(ReportGroup));
		modelBuilder.Entity<ProjectGroup>().ToTable(nameof(ProjectGroup));
		modelBuilder.Entity<Template>().ToTable(nameof(Template));
		modelBuilder.Entity<TemplateEntry>().ToTable(nameof(TemplateEntry));
		modelBuilder.Entity<TemplateGroup>().ToTable(nameof(TemplateGroup));
		modelBuilder.Entity<Transaction>().ToTable(nameof(Transaction));
		modelBuilder.Entity<TransactionEntry>().ToTable(nameof(TransactionEntry));
		modelBuilder.Entity<SystemConfig>().ToTable(nameof(SystemConfig));
		modelBuilder.Entity<LocalConfig>().ToTable(nameof(LocalConfig));
		modelBuilder.Entity<SystemConfig>().HasOne<Currency>().WithMany().HasForeignKey(config => config.BaseCurrencyId);
		modelBuilder.Entity<SystemConfig>().HasOne<Account>().WithMany().HasForeignKey(config => config.BalancingAccountId);
		modelBuilder.Entity<Transaction>().Property(entity => entity.Id)
			.HasConversion(value => value.ToByteArray(), value => new Guid(value)).HasColumnType("BLOB");
		modelBuilder.Entity<TransactionEntry>().Property(entity => entity.TransactionId)
			.HasConversion(value => value.ToByteArray(), value => new Guid(value)).HasColumnType("BLOB");
		modelBuilder.Entity<TransactionEntry>().Property(entity => entity.Amount)
			.HasConversion(value => ScaledAmount.ToStorage(value), value => ScaledAmount.FromStorage(value)).HasColumnType("INTEGER");
		modelBuilder.Entity<TransactionEntry>().Property(entity => entity.CumulativeAmount)
			.HasConversion(value => ScaledAmount.ToStorage(value), value => ScaledAmount.FromStorage(value)).HasColumnType("INTEGER");
		modelBuilder.Entity<TransactionEntry>().Property(entity => entity.Rate)
			.HasConversion(value => ScaledAmount.ToStorage(value), value => ScaledAmount.FromStorage(value)).HasColumnType("INTEGER");
		modelBuilder.Entity<CurrencyRate>().Property(entity => entity.Rate)
			.HasConversion(value => ScaledAmount.ToStorage(value), value => ScaledAmount.FromStorage(value)).HasColumnType("INTEGER");
		modelBuilder.Entity<TemplateEntry>().Property(entity => entity.Amount)
			.HasConversion(value => ScaledAmount.ToStorage(value), value => ScaledAmount.FromStorage(value)).HasColumnType("INTEGER");
		modelBuilder.Entity<Transaction>().HasIndex(entity => new { entity.DateTime, entity.Id });
		modelBuilder.Entity<TransactionEntry>().HasIndex(entity => new { entity.AccountId, entity.TransactionId, entity.Position });
	}
}
