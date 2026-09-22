using Business.Core.Entities;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;

#pragma warning disable CA1859

namespace DataAccess.EntityFramework;

/// <summary>
/// Defines the application persistence mapper.
/// Maps supported persistent business models to and from Entity Framework entities.
/// Repositories resolve the mapper through IMapper for single values and collections.
/// It isolates Business.Models from DataAccess.EntityFramework.Models.
/// Unsupported mappings fail explicitly instead of using reflection-based conventions.
/// </summary>
internal class AppMapper : IMapper
{
	public TDal Map<TDal, TBus>(TBus bus)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity, new()
	{
		ArgumentNullException.ThrowIfNull(bus);
		return null!;
	}

	public ICollection<TDal> Map<TDal, TBus>(ICollection<TBus> bus)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity, new()
	{
		ArgumentNullException.ThrowIfNull(bus);
		return [.. bus.Select(Map<TDal, TBus>)];
	}

	public TBus Map<TDal, TBus>(TDal dal)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity, new()
	{
		ArgumentNullException.ThrowIfNull(dal);
		return null!;
	}

	public ICollection<TBus> Map<TDal, TBus>(ICollection<TDal> dal)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity, new()
	{
		ArgumentNullException.ThrowIfNull(dal);
		return [.. dal.Select(Map<TDal, TBus>)];
	}
}
