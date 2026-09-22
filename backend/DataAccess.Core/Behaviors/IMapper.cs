using Business.Core.Entities;
using DataAccess.Core.Entities;

namespace DataAccess.Core.Behaviors;

public interface IMapper
{
	public TDal Map<TDal, TBus>(TBus bus)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity, new();

	public ICollection<TDal> Map<TDal, TBus>(ICollection<TBus> bus)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity, new();

	public TBus Map<TDal, TBus>(TDal dal)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity, new();

	public ICollection<TBus> Map<TDal, TBus>(ICollection<TDal> dal)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity, new();
}
