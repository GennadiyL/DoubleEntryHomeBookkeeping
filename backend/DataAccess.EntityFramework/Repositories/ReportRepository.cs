using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.EntityFramework.Models;
using DataAccess.EntityFramework.Repositories.Base;
using ReportEntity = Business.Models.Entities.Reporting.Report;
using ReportGroupEntity = Business.Models.Entities.Reporting.ReportGroup;

namespace DataAccess.EntityFramework.Repositories;

/// <summary>
/// Persists the report catalog through the shared EF repository behavior.
/// Exposes business entities while keeping DAL models internal.
/// Maps write inputs to scalar values and foreign keys only.
/// Read operations return detached business models.
/// Included relationships are mapped from the loaded graph without extra queries.
/// Participates in the caller's scoped unit of work and transaction.
/// Business services own validation, ordering decisions and deletion policy.
/// Saved instructions are stored without interpretation or report calculation.
/// </summary>
internal sealed class ReportRepository : ElementRepository<ReportGroupEntity, ReportEntity, Report>, IReportRepository
{
	public ReportRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
