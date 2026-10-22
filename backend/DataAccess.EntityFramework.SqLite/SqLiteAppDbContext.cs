using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.SqLite.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Applies SQLite-specific storage mappings to the complete persistence model.
/// Entry amounts and cumulative amounts use scaled integers.
/// All Guid properties use parameterless Guid.ToByteArray bytes.
/// Required and nullable foreign keys use the same identity representation.
/// Database ordering therefore matches cumulative tie-breaking.
/// String dataset keys retain their existing text representation.
/// Indexes support account membership and transaction-date lookups.
/// Existing databases require explicit schema migration before use.
/// </summary>
public abstract class SqLiteAppDbContext : AppDbContext
{
	protected SqLiteAppDbContext(DbContextOptions options) : base(options)
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
		modelBuilder.Entity<LocalRegistration>().ToTable(nameof(LocalRegistration));
		modelBuilder.Entity<LocalRegistration>().HasIndex(entity => entity.LocalDatasetKey).IsUnique();
		modelBuilder.Entity<Transaction>().Property(entity => entity.DateTime)
			.HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
		modelBuilder.Entity<LocalRegistration>().Property(entity => entity.SyncDateTime)
			.HasConversion(value => value, value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null);
		modelBuilder.Entity<LocalSyncBatch>().ToTable(nameof(LocalSyncBatch));
		modelBuilder.Entity<LocalSyncBatch>().HasOne(entity => entity.LocalRegistration).WithMany()
			.HasForeignKey(entity => entity.LocalRegistrationId).OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<LocalSyncBatch>().HasIndex(entity => new { entity.LocalRegistrationId, entity.SyncKey }).IsUnique();
		foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
		{
			foreach (IMutableProperty property in entityType.GetProperties())
			{
				if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(Guid))
				{
					property.SetValueConverter(new GuidToBytesConverter());
					property.SetColumnType("BLOB");
				}
			}
		}
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
