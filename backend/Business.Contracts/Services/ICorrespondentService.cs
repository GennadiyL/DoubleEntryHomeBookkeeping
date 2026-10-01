using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICorrespondentService :
	IElementService<CorrespondentGroup, Correspondent>,
	IUpdateEntityService<ElementParam>,
	IReadEntityService<ElementInfo>
{
}
