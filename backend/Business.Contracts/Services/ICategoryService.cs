using Business.Contracts.Params;
using Business.Contracts.Services.Base;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICategoryService : IElementService<CategoryGroup, Category, ElementParam>
{
}
