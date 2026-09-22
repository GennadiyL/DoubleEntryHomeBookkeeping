using Business.Core.Entities;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;
using DataAccess.EntityFramework.Models;
using AccountEntity = Business.Models.Entities.Account;
using AccountGroupEntity = Business.Models.Entities.AccountGroup;
using CategoryEntity = Business.Models.Entities.Category;
using CategoryGroupEntity = Business.Models.Entities.CategoryGroup;
using CorrespondentEntity = Business.Models.Entities.Correspondent;
using CorrespondentGroupEntity = Business.Models.Entities.CorrespondentGroup;
using CurrencyEntity = Business.Models.Entities.Currency;
using CurrencyRateEntity = Business.Models.Entities.CurrencyRate;
using ProjectEntity = Business.Models.Entities.Project;
using ProjectGroupEntity = Business.Models.Entities.ProjectGroup;
using SystemConfigEntity = Business.Models.Entities.Config.SystemConfig;
using TemplateEntity = Business.Models.Entities.Template;
using TemplateEntryEntity = Business.Models.Entities.TemplateEntry;
using TemplateGroupEntity = Business.Models.Entities.TemplateGroup;
using TransactionEntity = Business.Models.Entities.Transaction;
using TransactionEntryEntity = Business.Models.Entities.TransactionEntry;
using UserConfigEntity = Business.Models.Entities.Config.UserConfig;

namespace DataAccess.EntityFramework;

/// <summary>
/// Maps each supported persistent business entity to its DAL representation.
/// Uses explicit type dispatch and property assignments for all entity types.
/// Business-to-DAL mapping copies scalar values and foreign keys only.
/// DAL-to-business mapping preserves the complete graph already loaded.
/// A per-call reference cache preserves shared references and breaks cycles.
/// Collection reads share the cache across every item in the result.
/// Mapping performs no database queries or business validation.
/// Unsupported source and destination combinations fail explicitly.
/// </summary>
internal class AppMapper : IMapper
{
	public TDal Map<TDal, TBus>(TBus bus)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity
	{
		ArgumentNullException.ThrowIfNull(bus);
		return MapBusinessToDal(bus) is TDal mapped
			? mapped
			: throw UnsupportedMapping(typeof(TBus), typeof(TDal));
	}

	public ICollection<TDal> Map<TDal, TBus>(ICollection<TBus> bus)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity
	{
		ArgumentNullException.ThrowIfNull(bus);
		return [.. bus.Select(Map<TDal, TBus>)];
	}

	public TBus Map<TDal, TBus>(TDal dal)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity
	{
		ArgumentNullException.ThrowIfNull(dal);
		Dictionary<IDalEntity, IBaseEntity> cache = new(ReferenceEqualityComparer.Instance);
		return MapDalToBusiness(dal, cache) is TBus mapped
			? mapped
			: throw UnsupportedMapping(typeof(TDal), typeof(TBus));
	}

	public ICollection<TBus> Map<TDal, TBus>(ICollection<TDal> dal)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity
	{
		ArgumentNullException.ThrowIfNull(dal);
		Dictionary<IDalEntity, IBaseEntity> cache = new(ReferenceEqualityComparer.Instance);
		return [.. dal.Select(value =>
		{
			ArgumentNullException.ThrowIfNull(value);
			return MapDalToBusiness(value, cache) is TBus mapped
				? mapped
				: throw UnsupportedMapping(typeof(TDal), typeof(TBus));
		})];
	}

	private static ArgumentException UnsupportedMapping(Type sourceType, Type destinationContract) =>
		new($"No mapping is configured from '{sourceType.FullName}' to '{destinationContract.Name}'.",
			nameof(sourceType));

	private static IDalEntity MapBusinessToDal(IBaseEntity entity) => entity switch
	{
		AccountEntity value => Map(value),
		AccountGroupEntity value => Map(value),
		CategoryEntity value => Map(value),
		CategoryGroupEntity value => Map(value),
		CorrespondentEntity value => Map(value),
		CorrespondentGroupEntity value => Map(value),
		CurrencyEntity value => Map(value),
		CurrencyRateEntity value => Map(value),
		ProjectEntity value => Map(value),
		ProjectGroupEntity value => Map(value),
		SystemConfigEntity value => Map(value),
		TemplateEntity value => Map(value),
		TemplateEntryEntity value => Map(value),
		TemplateGroupEntity value => Map(value),
		TransactionEntity value => Map(value),
		TransactionEntryEntity value => Map(value),
		UserConfigEntity value => Map(value),
		_ => throw UnsupportedMapping(entity.GetType(), typeof(IDalEntity))
	};

	private static IBaseEntity MapDalToBusiness(IDalEntity entity, Dictionary<IDalEntity, IBaseEntity> cache) => entity switch
	{
		Account value => Map(value, cache),
		AccountGroup value => Map(value, cache),
		Category value => Map(value, cache),
		CategoryGroup value => Map(value, cache),
		Correspondent value => Map(value, cache),
		CorrespondentGroup value => Map(value, cache),
		Currency value => Map(value, cache),
		CurrencyRate value => Map(value, cache),
		Project value => Map(value, cache),
		ProjectGroup value => Map(value, cache),
		SystemConfig value => Map(value, cache),
		Template value => Map(value, cache),
		TemplateEntry value => Map(value, cache),
		TemplateGroup value => Map(value, cache),
		Transaction value => Map(value, cache),
		TransactionEntry value => Map(value, cache),
		UserConfig value => Map(value, cache),
		_ => throw UnsupportedMapping(entity.GetType(), typeof(IBaseEntity))
	};

	private static Account Map(AccountEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		GroupId = value.GroupId,
		CurrencyId = value.CurrencyId,
		CategoryId = value.CategoryId,
		CorrespondentId = value.CorrespondentId,
		ProjectId = value.ProjectId
	};

	private static AccountEntity Map(Account value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (AccountEntity)existing;
		}

		AccountEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			GroupId = value.GroupId,
			CurrencyId = value.CurrencyId,
			CategoryId = value.CategoryId,
			CorrespondentId = value.CorrespondentId,
			ProjectId = value.ProjectId,
			Group = null!,
			Currency = null!,
			Category = null!,
			Correspondent = null!,
			Project = null!
		};
		cache.Add(value, mapped);
		mapped.Group = value.Group is null ? null! : Map(value.Group, cache);
		mapped.Currency = value.Currency is null ? null! : Map(value.Currency, cache);
		mapped.Category = value.Category is null ? null : Map(value.Category, cache);
		mapped.Correspondent = value.Correspondent is null ? null : Map(value.Correspondent, cache);
		mapped.Project = value.Project is null ? null : Map(value.Project, cache);
		return mapped;
	}

	private static AccountGroup Map(AccountGroupEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		ParentId = value.ParentId
	};

	private static AccountGroupEntity Map(AccountGroup value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (AccountGroupEntity)existing;
		}

		AccountGroupEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			ParentId = value.ParentId,
			Parent = null!
		};
		cache.Add(value, mapped);
		mapped.Parent = value.Parent is null ? null! : Map(value.Parent, cache);
		mapped.Children = [.. value.Children.Select(item => Map(item, cache))];
		mapped.Elements = [.. value.Elements.Select(item => Map(item, cache))];
		return mapped;
	}

	private static Category Map(CategoryEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		GroupId = value.GroupId
	};

	private static CategoryEntity Map(Category value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (CategoryEntity)existing;
		}

		CategoryEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			GroupId = value.GroupId,
			Group = null!
		};
		cache.Add(value, mapped);
		mapped.Group = value.Group is null ? null! : Map(value.Group, cache);
		return mapped;
	}

	private static CategoryGroup Map(CategoryGroupEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		ParentId = value.ParentId
	};

	private static CategoryGroupEntity Map(CategoryGroup value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (CategoryGroupEntity)existing;
		}

		CategoryGroupEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			ParentId = value.ParentId,
			Parent = null!
		};
		cache.Add(value, mapped);
		mapped.Parent = value.Parent is null ? null! : Map(value.Parent, cache);
		mapped.Children = [.. value.Children.Select(item => Map(item, cache))];
		mapped.Elements = [.. value.Elements.Select(item => Map(item, cache))];
		return mapped;
	}

	private static Correspondent Map(CorrespondentEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		GroupId = value.GroupId
	};

	private static CorrespondentEntity Map(Correspondent value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (CorrespondentEntity)existing;
		}

		CorrespondentEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			GroupId = value.GroupId,
			Group = null!
		};
		cache.Add(value, mapped);
		mapped.Group = value.Group is null ? null! : Map(value.Group, cache);
		return mapped;
	}

	private static CorrespondentGroup Map(CorrespondentGroupEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		ParentId = value.ParentId
	};

	private static CorrespondentGroupEntity Map(CorrespondentGroup value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (CorrespondentGroupEntity)existing;
		}

		CorrespondentGroupEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			ParentId = value.ParentId,
			Parent = null!
		};
		cache.Add(value, mapped);
		mapped.Parent = value.Parent is null ? null! : Map(value.Parent, cache);
		mapped.Children = [.. value.Children.Select(item => Map(item, cache))];
		mapped.Elements = [.. value.Elements.Select(item => Map(item, cache))];
		return mapped;
	}

	private static Currency Map(CurrencyEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		IsoCode = value.IsoCode,
		Symbol = value.Symbol,
		Name = value.Name,
		IsFavorite = value.IsFavorite,
		Order = value.Order
	};

	private static CurrencyEntity Map(Currency value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (CurrencyEntity)existing;
		}

		CurrencyEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			IsoCode = value.IsoCode,
			Symbol = value.Symbol,
			Name = value.Name,
			IsFavorite = value.IsFavorite,
			Order = value.Order
		};
		cache.Add(value, mapped);
		mapped.Rates = [.. value.Rates.Select(item => Map(item, cache))];
		return mapped;
	}

	private static CurrencyRate Map(CurrencyRateEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		CurrencyId = value.CurrencyId,
		Date = value.Date,
		Rate = value.Rate,
		Comment = value.Comment
	};

	private static CurrencyRateEntity Map(CurrencyRate value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (CurrencyRateEntity)existing;
		}

		CurrencyRateEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			CurrencyId = value.CurrencyId,
			Date = value.Date,
			Rate = value.Rate,
			Comment = value.Comment,
			Currency = null!
		};
		cache.Add(value, mapped);
		mapped.Currency = value.Currency is null ? null! : Map(value.Currency, cache);
		return mapped;
	}

	private static Project Map(ProjectEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		GroupId = value.GroupId
	};

	private static ProjectEntity Map(Project value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (ProjectEntity)existing;
		}

		ProjectEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			GroupId = value.GroupId,
			Group = null!
		};
		cache.Add(value, mapped);
		mapped.Group = value.Group is null ? null! : Map(value.Group, cache);
		return mapped;
	}

	private static ProjectGroup Map(ProjectGroupEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		ParentId = value.ParentId
	};

	private static ProjectGroupEntity Map(ProjectGroup value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (ProjectGroupEntity)existing;
		}

		ProjectGroupEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			ParentId = value.ParentId,
			Parent = null!
		};
		cache.Add(value, mapped);
		mapped.Parent = value.Parent is null ? null! : Map(value.Parent, cache);
		mapped.Children = [.. value.Children.Select(item => Map(item, cache))];
		mapped.Elements = [.. value.Elements.Select(item => Map(item, cache))];
		return mapped;
	}

	private static SystemConfig Map(SystemConfigEntity value) => new()
	{
		Id = value.Id,
		MainCurrencyIsoCode = value.MainCurrencyIsoCode,
		MinDate = value.MinDate,
		MaxDate = value.MaxDate
	};

	private static SystemConfigEntity Map(SystemConfig value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (SystemConfigEntity)existing;
		}

		SystemConfigEntity mapped = new()
		{
			Id = value.Id,
			MainCurrencyIsoCode = value.MainCurrencyIsoCode,
			MinDate = value.MinDate,
			MaxDate = value.MaxDate
		};
		cache.Add(value, mapped);
		return mapped;
	}

	private static Template Map(TemplateEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		GroupId = value.GroupId
	};

	private static TemplateEntity Map(Template value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (TemplateEntity)existing;
		}

		TemplateEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			GroupId = value.GroupId,
			Group = null!
		};
		cache.Add(value, mapped);
		mapped.Group = value.Group is null ? null! : Map(value.Group, cache);
		mapped.Entries = [.. value.Entries.Select(item => Map(item, cache))];
		return mapped;
	}

	private static TemplateEntry Map(TemplateEntryEntity value) => new()
	{
		Id = value.Id,
		TemplateId = value.TemplateId,
		AccountId = value.AccountId,
		Amount = value.Amount
	};

	private static TemplateEntryEntity Map(TemplateEntry value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (TemplateEntryEntity)existing;
		}

		TemplateEntryEntity mapped = new()
		{
			Id = value.Id,
			TemplateId = value.TemplateId,
			AccountId = value.AccountId,
			Amount = value.Amount,
			Template = null!,
			Account = null!
		};
		cache.Add(value, mapped);
		mapped.Template = value.Template is null ? null! : Map(value.Template, cache);
		mapped.Account = value.Account is null ? null! : Map(value.Account, cache);
		return mapped;
	}

	private static TemplateGroup Map(TemplateGroupEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		Name = value.Name,
		Description = value.Description,
		Order = value.Order,
		IsFavorite = value.IsFavorite,
		ParentId = value.ParentId
	};

	private static TemplateGroupEntity Map(TemplateGroup value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (TemplateGroupEntity)existing;
		}

		TemplateGroupEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			Name = value.Name,
			Description = value.Description,
			Order = value.Order,
			IsFavorite = value.IsFavorite,
			ParentId = value.ParentId,
			Parent = null!
		};
		cache.Add(value, mapped);
		mapped.Parent = value.Parent is null ? null! : Map(value.Parent, cache);
		mapped.Children = [.. value.Children.Select(item => Map(item, cache))];
		mapped.Elements = [.. value.Elements.Select(item => Map(item, cache))];
		return mapped;
	}

	private static Transaction Map(TransactionEntity value) => new()
	{
		Id = value.Id,
		Original = value.Original,
		Current = value.Current,
		IsDeleted = value.IsDeleted,
		DateTime = value.DateTime,
		State = value.State,
		Comment = value.Comment
	};

	private static TransactionEntity Map(Transaction value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (TransactionEntity)existing;
		}

		TransactionEntity mapped = new()
		{
			Id = value.Id,
			Original = value.Original,
			Current = value.Current,
			IsDeleted = value.IsDeleted,
			DateTime = value.DateTime,
			State = value.State,
			Comment = value.Comment
		};
		cache.Add(value, mapped);
		mapped.Entries = [.. value.Entries.Select(item => Map(item, cache))];
		return mapped;
	}

	private static TransactionEntry Map(TransactionEntryEntity value) => new()
	{
		Id = value.Id,
		TransactionId = value.TransactionId,
		AccountId = value.AccountId,
		Amount = value.Amount,
		Rate = value.Rate
	};

	private static TransactionEntryEntity Map(TransactionEntry value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (TransactionEntryEntity)existing;
		}

		TransactionEntryEntity mapped = new()
		{
			Id = value.Id,
			TransactionId = value.TransactionId,
			AccountId = value.AccountId,
			Amount = value.Amount,
			Rate = value.Rate,
			Transaction = null!,
			Account = null!
		};
		cache.Add(value, mapped);
		mapped.Transaction = value.Transaction is null ? null! : Map(value.Transaction, cache);
		mapped.Account = value.Account is null ? null! : Map(value.Account, cache);
		return mapped;
	}

	private static UserConfig Map(UserConfigEntity value) => new()
	{
		Id = value.Id
	};

	private static UserConfigEntity Map(UserConfig value, Dictionary<IDalEntity, IBaseEntity> cache)
	{
		if (cache.TryGetValue(value, out IBaseEntity? existing))
		{
			return (UserConfigEntity)existing;
		}

		UserConfigEntity mapped = new()
		{
			Id = value.Id
		};
		cache.Add(value, mapped);
		return mapped;
	}
}
