using Business.Models.Entities;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ITemplateGroupRepository : IGroupRepository<TemplateGroup, Template>
{
}

