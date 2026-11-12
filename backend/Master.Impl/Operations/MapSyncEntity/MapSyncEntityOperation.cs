using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Core.Entities;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using Exchange.Models.Synchronizations;
using Master.Impl.Operations.ApplySyncBatch;

namespace Master.Impl.Operations.MapSyncEntity;

/// <summary>
/// Maps captured payloads and detached entities through an explicit property allowlist.
/// Receives mapping metadata and entity data through method parameters.
/// Creates only anemic business entities from registered CLR types.
/// Aggregate entry mappings exclude navigation and derived cache values.
/// Snapshots support content comparison without retaining invocation data.
/// No repository callbacks or entity collections are held by the implementation.
/// Loading, saving and transaction ownership remain with the caller.
/// The Master module supplies this operation through dependency injection.
/// </summary>
internal sealed class MapSyncEntityOperation : IMapSyncEntityOperation
{
	private static readonly JsonSerializerOptions ReadOptions = new() { Converters = { new JsonStringEnumConverter() } };

	public void RememberAggregate(SyncEntityState set, SyncMergeRow row)
	{
		string field = EntriesField(row.Entity) ?? throw new InvalidOperationException("Unsupported aggregate.");
		row.Original![field] = Snapshot(set, row.Entity)[field];
	}

	public BaseEntity Read(SyncEntityState set, SyncObject item, bool content, bool order)
	{
		Dictionary<string, PropertyInfo> fields = GetFields(set);
		HashSet<string> expected = content ? new(fields.Keys, StringComparer.Ordinal) : new(StringComparer.Ordinal);
		if (order) { expected.Add(nameof(IOrderedEntity.Order)); }
		if (!expected.SetEquals(item.Properties.Keys))
		{
			throw new InvalidSyncBatchException($"{set.Type}/{item.Id}: incomplete or unsupported properties.");
		}
		BaseEntity entity = (BaseEntity)Activator.CreateInstance(set.EntityType)!;
		entity.Id = item.Id;
		try
		{
			foreach (string name in expected)
			{
				JsonElement value = item.Properties[name];
				if (name == nameof(IOrderedEntity.Order))
				{
					((IOrderedEntity)entity).Order = value.GetInt32();
				}
				else if (name == EntriesField(entity))
				{
					ReadEntries(entity, value);
				}
				else
				{
					PropertyInfo field = fields[name];
					object? parsed = value.Deserialize(field.PropertyType, ReadOptions);
					if (parsed is null && name is not (nameof(CatalogEntity.Description) or nameof(Account.CategoryId) or nameof(Account.CorrespondentId) or nameof(Account.ProjectId)))
					{
						throw new InvalidSyncBatchException($"{set.Type}/{item.Id}: {name} cannot be null.");
					}
					field.SetValue(entity, parsed);
				}
			}
		}
		catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or OverflowException)
		{
			throw new InvalidSyncBatchException($"{set.Type}/{item.Id}: invalid property value ({exception.Message}).");
		}
		return entity;
	}

	public Dictionary<string, JsonElement> Snapshot(SyncEntityState set, BaseEntity entity)
	{
		Dictionary<string, PropertyInfo> fields = GetFields(set);
		return fields.ToDictionary(field => field.Key, field => field.Key == EntriesField(entity)
			? Entries(entity) : JsonSerializer.SerializeToElement(field.Value.GetValue(entity), field.Value.PropertyType));
	}

	public bool ContentChanged(SyncEntityState set, SyncMergeRow row)
		=> row.Original is null || Snapshot(set, row.Entity).Any(field => !JsonElement.DeepEquals(field.Value, row.Original[field.Key]));

	private static string? EntriesField(BaseEntity entity) => entity switch
	{
		Transaction => nameof(Transaction.Entries),
		Template => nameof(Template.Entries),
		_ => null
	};

	private static JsonElement Entries(BaseEntity entity) => entity switch
	{
		Transaction transaction => JsonSerializer.SerializeToElement(transaction.Entries.OrderBy(entry => entry.Position)
			.Select(entry => new { entry.Id, entry.AccountId, entry.Position, entry.Amount, entry.Rate })),
		Template template => JsonSerializer.SerializeToElement(template.Entries.OrderBy(entry => entry.Position)
			.Select(entry => new { entry.Id, entry.AccountId, entry.Position, entry.Amount })),
		_ => throw new InvalidOperationException("Unsupported aggregate.")
	};

	private static void ReadEntries(BaseEntity entity, JsonElement entries)
	{
		HashSet<Guid> ids = new();
		int position = 0;
		(string Id, string AccountId, string Position, string Amount) names = entity switch
		{
			Transaction => (nameof(TransactionEntry.Id), nameof(TransactionEntry.AccountId), nameof(TransactionEntry.Position), nameof(TransactionEntry.Amount)),
			Template => (nameof(TemplateEntry.Id), nameof(TemplateEntry.AccountId), nameof(TemplateEntry.Position), nameof(TemplateEntry.Amount)),
			_ => throw new InvalidOperationException("Unsupported aggregate.")
		};
		string[] fields = entity is Transaction
			? [names.Id, names.AccountId, names.Position, names.Amount, nameof(TransactionEntry.Rate)]
			: [names.Id, names.AccountId, names.Position, names.Amount];
		foreach (JsonElement entry in entries.EnumerateArray())
		{
			if (!fields.ToHashSet(StringComparer.Ordinal).SetEquals(entry.EnumerateObject().Select(property => property.Name)) ||
				entry.EnumerateObject().Count() != fields.Length)
			{
				throw new InvalidSyncBatchException("An aggregate entry has incomplete or unsupported properties.");
			}
			Guid id = entry.GetProperty(names.Id).GetGuid();
			Guid accountId = entry.GetProperty(names.AccountId).GetGuid();
			if (id == Guid.Empty || accountId == Guid.Empty || !ids.Add(id) || entry.GetProperty(names.Position).GetInt32() != position)
			{
				throw new InvalidSyncBatchException("Entry identities must be nonempty and unique; positions must be consecutive from zero.");
			}
			decimal amount = entry.GetProperty(names.Amount).GetDecimal();
			if (entity is Transaction transaction)
			{
				transaction.Entries.Add(new TransactionEntry
				{
					Id = id, TransactionId = entity.Id, Transaction = transaction, AccountId = accountId,
					Account = null!, Amount = amount, Position = position, Rate = entry.GetProperty(nameof(TransactionEntry.Rate)).GetDecimal()
				});
			}
			else if (entity is Template template)
			{
				template.Entries.Add(new TemplateEntry
				{
					Id = id, TemplateId = entity.Id, Template = template, AccountId = accountId,
					Account = null!, Amount = amount, Position = position
				});
			}
			position++;
		}
	}

	private static Dictionary<string, PropertyInfo> GetFields(SyncEntityState set)
		=> set.ContentFields.ToDictionary(name => name, name => set.EntityType.GetProperty(name)
			?? throw new InvalidOperationException($"Missing mapped field {name}."));
}
