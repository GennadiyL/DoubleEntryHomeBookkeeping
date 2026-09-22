using System.Collections;
using System.Reflection;
using Business.Core.Entities;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;
using Dal = DataAccess.EntityFramework.Models;
using Bus = Business.Models.Entities;
using Config = Business.Models.Entities.Config;

namespace DataAccess.EntityFramework;

internal class AppMapper : IMapper
{
	private static readonly IReadOnlyDictionary<Type, Type> BusinessToDal = new Dictionary<Type, Type>
	{
		[typeof(Bus.Account)] = typeof(Dal.Account),
		[typeof(Bus.AccountGroup)] = typeof(Dal.AccountGroup),
		[typeof(Bus.Category)] = typeof(Dal.Category),
		[typeof(Bus.CategoryGroup)] = typeof(Dal.CategoryGroup),
		[typeof(Bus.Correspondent)] = typeof(Dal.Correspondent),
		[typeof(Bus.CorrespondentGroup)] = typeof(Dal.CorrespondentGroup),
		[typeof(Bus.Currency)] = typeof(Dal.Currency),
		[typeof(Bus.CurrencyRate)] = typeof(Dal.CurrencyRate),
		[typeof(Bus.Project)] = typeof(Dal.Project),
		[typeof(Bus.ProjectGroup)] = typeof(Dal.ProjectGroup),
		[typeof(Bus.Template)] = typeof(Dal.Template),
		[typeof(Bus.TemplateEntry)] = typeof(Dal.TemplateEntry),
		[typeof(Bus.TemplateGroup)] = typeof(Dal.TemplateGroup),
		[typeof(Bus.Transaction)] = typeof(Dal.Transaction),
		[typeof(Bus.TransactionEntry)] = typeof(Dal.TransactionEntry),
		[typeof(Config.SystemConfig)] = typeof(Dal.SystemConfig),
		[typeof(Config.UserConfig)] = typeof(Dal.UserConfig)
	};

	private static readonly Dictionary<Type, Type> DalToBusiness =
		BusinessToDal.ToDictionary(x => x.Value, x => x.Key);

	public TDal Map<TDal, TBus>(TBus bus)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity =>
		(TDal)MapObject(bus, typeof(TDal), false, new Dictionary<object, object>(ReferenceEqualityComparer.Instance));

	public ICollection<TDal> Map<TDal, TBus>(ICollection<TBus> bus)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity => [.. bus.Select(Map<TDal, TBus>)];

	public TBus Map<TDal, TBus>(TDal dal)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity =>
		(TBus)MapObject(dal, typeof(TBus), true, new Dictionary<object, object>(ReferenceEqualityComparer.Instance));

	public ICollection<TBus> Map<TDal, TBus>(ICollection<TDal> dal)
		where TDal : class, IDalEntity, new()
		where TBus : class, IBaseEntity => [.. dal.Select(Map<TDal, TBus>)];

	private static object MapObject(object source, Type destinationType, bool mapGraph, IDictionary<object, object> cache)
	{
		ArgumentNullException.ThrowIfNull(source);
		Type sourceType = source.GetType();
		IReadOnlyDictionary<Type, Type> supported = mapGraph ? DalToBusiness : BusinessToDal;
		if (!supported.TryGetValue(sourceType, out Type? configuredType) || configuredType != destinationType)
		{
			throw new ArgumentException($"No mapping is configured from '{sourceType.FullName}' to '{destinationType.FullName}'.", nameof(source));
		}

		if (cache.TryGetValue(source, out object? existing))
		{
			return existing;
		}

		object destination = Activator.CreateInstance(destinationType)!;
		cache[source] = destination;
		foreach (PropertyInfo sourceProperty in sourceType.GetProperties().Where(x => x.CanRead))
		{
			PropertyInfo? destinationProperty = destinationType.GetProperty(sourceProperty.Name);
			if (destinationProperty?.CanWrite != true)
			{
				continue;
			}

			object? value = sourceProperty.GetValue(source);
			if (value == null)
			{
				continue;
			}

			if (IsScalar(destinationProperty.PropertyType))
				destinationProperty.SetValue(destination, value);
			else if (mapGraph && DalToBusiness.TryGetValue(value.GetType(), out Type? navigationType))
				destinationProperty.SetValue(destination, MapObject(value, navigationType, true, cache));
			else if (mapGraph && value is IEnumerable values && TryGetElementType(destinationProperty.PropertyType, out Type? elementType))
			{
				IList collection = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
				foreach (object item in values)
					collection.Add(MapObject(item, DalToBusiness[item.GetType()], true, cache));
				destinationProperty.SetValue(destination, collection);
			}
		}
		return destination;
	}

	private static bool IsScalar(Type type)
	{
		Type actual = Nullable.GetUnderlyingType(type) ?? type;
		return actual.IsPrimitive || actual.IsEnum || actual == typeof(string) || actual == typeof(Guid) ||
			actual == typeof(decimal) || actual == typeof(DateTime) || actual == typeof(DateOnly);
	}

	private static bool TryGetElementType(Type type, out Type elementType)
	{
		Type? enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
			? type : type.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
		elementType = enumerable?.GetGenericArguments()[0]!;
		return enumerable != null;
	}
}
