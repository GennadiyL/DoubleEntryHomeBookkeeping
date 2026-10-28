using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Applies the existing SQLite model to the common AppDbContext.
/// Factory contexts select this service through their EF options.
/// Role-specific context subclasses are not needed on the new path.
/// DbSet discovery still uses the normal EF model customization.
/// SQLite mappings are shared with the existing context implementation.
/// The service holds no database identity or connection state.
/// Every factory role receives the same persistence model.
/// Legacy contexts retain their existing model-building entry point.
/// </summary>
internal sealed class SqLiteAppModelCustomizer : ModelCustomizer
{
	public SqLiteAppModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
	{
	}

	public override void Customize(ModelBuilder modelBuilder, DbContext context)
	{
		base.Customize(modelBuilder, context);
		SqLiteAppDbContext.ConfigureModel(modelBuilder);
	}
}
