using Business.Models.Entities.Reporting;
using DataAccess.Contracts.Repositories.Base;

namespace DataAccess.Contracts.Repositories;

public interface IReportGroupRepository : IGroupRepository<ReportGroup, Report>
{
}
