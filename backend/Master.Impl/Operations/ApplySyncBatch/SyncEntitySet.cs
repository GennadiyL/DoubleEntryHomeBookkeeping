using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Core.Entities;
using Business.Models.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using DataAccess.Contracts.Repositories.Base;
using Exchange.Models.Synchronizations;

namespace Master.Impl.Operations.ApplySyncBatch;

/// <summary>
/// Maps one explicitly registered exchange type to a tracked repository.
/// Only the configured scalar property allowlist can be supplied by a client.
/// Reflection reads that fixed model schema, never a client-provided CLR type.
/// Aggregate entries have explicit mappings excluding navigation and derived values.
/// Loaded entities remain detached until the operation finishes reconciliation.
/// Original snapshots support semantic content comparison and minimal persistence.
/// Order and synchronization metadata are outside the content allowlist.
/// Repository callbacks retain the strongly typed business model boundary.
/// </summary>
internal sealed class SyncEntitySet
{
	private static readonly JsonSerializerOptions _readOptions = new() { Converters = { new JsonStringEnumConverter() } };
	private readonly Type _entityType;
	private readonly Dictionary<string, PropertyInfo> _fields;
	private readonly Func<CancellationToken, Task<ICollection<BaseEntity>>> _load;
	private readonly Action<BaseEntity> _add;
	private readonly Action<BaseEntity> _update;

	private SyncEntitySet(string type, Type entityType, string[] fields,
		Func<CancellationToken, Task<ICollection<BaseEntity>>> load, Action<BaseEntity> add, Action<BaseEntity> update)
	{
		Type = type;
		_entityType = entityType;
		_fields = fields.ToDictionary(name => JsonNamingPolicy.CamelCase.ConvertName(name),
			name => entityType.GetProperty(name) ?? throw new InvalidOperationException($"Missing mapped field {name}."));
		_load = load;
		_add = add;
		_update = update;
	}

	public string Type { get; }
	public Guid? RootId { get; init; }
	public string? ParentType { get; init; }
	public string? ParentField { get; init; }
	public Dictionary<Guid, SyncMergeRow> Rows { get; } = new();
	public bool IsOrdered => typeof(IOrderedEntity).IsAssignableFrom(_entityType);

	public static SyncEntitySet Create<T>(string type, ITrackedRepository<T> repository, string[] fields,
		Guid? rootId = null, string? parentType = null, string? parentField = null)
		where T : BaseEntity, ITrackedEntity
		=> new(type, typeof(T), fields,
			async token => (await repository.GetAll(token)).Cast<BaseEntity>().ToList(),
			entity => repository.Add((T)entity), entity => repository.Update((T)entity))
		{ RootId = rootId, ParentType = parentType, ParentField = parentField };

	public async Task Load(CancellationToken cancellationToken)
	{
		foreach (BaseEntity entity in await _load(cancellationToken))
		{
			ITrackedEntity tracked = (ITrackedEntity)entity;
			Rows.Add(entity.Id, new SyncMergeRow
			{
				Entity = entity, Original = Snapshot(entity), OriginalEditRevision = tracked.EditRevision,
				OriginalDeleteRevision = tracked.DeleteRevision, OriginalOrder = (entity as IOrderedEntity)?.Order
			});
		}
	}

	public void RememberAggregate(SyncMergeRow row)
	{
		row.Original!["entries"] = Snapshot(row.Entity)["entries"];
	}

	public BaseEntity Read(SyncObject item, bool content, bool order)
	{
		HashSet<string> expected = content ? new(_fields.Keys, StringComparer.Ordinal) : new(StringComparer.Ordinal);
		if (order) { expected.Add("order"); }
		if (!expected.SetEquals(item.Properties.Keys))
		{
			throw new InvalidSyncBatchException($"{Type}/{item.Id}: incomplete or unsupported properties.");
		}
		BaseEntity entity = (BaseEntity)Activator.CreateInstance(_entityType)!;
		entity.Id = item.Id;
		try
		{
			foreach (string name in expected)
			{
				JsonElement value = item.Properties[name];
				if (name == "order")
				{
					((IOrderedEntity)entity).Order = value.GetInt32();
				}
				else if (name == "entries")
				{
					ReadEntries(entity, value);
				}
				else
				{
					PropertyInfo field = _fields[name];
					object? parsed = value.Deserialize(field.PropertyType, _readOptions);
					if (parsed is null && name is not ("description" or "categoryId" or "correspondentId" or "projectId"))
					{
						throw new InvalidSyncBatchException($"{Type}/{item.Id}: {name} cannot be null.");
					}
					field.SetValue(entity, parsed);
				}
			}
		}
		catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or OverflowException)
		{
			throw new InvalidSyncBatchException($"{Type}/{item.Id}: invalid property value ({exception.Message}).");
		}
		return entity;
	}

	public Dictionary<string, JsonElement> Snapshot(BaseEntity entity)
	{
		return _fields.ToDictionary(field => field.Key, field => field.Key == "entries"
			? Entries(entity) : JsonSerializer.SerializeToElement(field.Value.GetValue(entity), field.Value.PropertyType));
	}

	public bool ContentChanged(SyncMergeRow row)
		=> row.Original is null || Snapshot(row.Entity).Any(field => !JsonElement.DeepEquals(field.Value, row.Original[field.Key]));

	public Guid ParentId(SyncMergeRow row) => (Guid)_fields[ParentField!].GetValue(row.Entity)!;

	public void SetParentId(SyncMergeRow row, Guid id) => _fields[ParentField!].SetValue(row.Entity, id);

	public void Persist(SyncMergeRow row)
	{
		if (row.IsNew) { _add(row.Entity); }
		else { _update(row.Entity); }
	}

	private static JsonElement Entries(BaseEntity entity) => entity switch
	{
		Transaction transaction => JsonSerializer.SerializeToElement(transaction.Entries.OrderBy(entry => entry.Position)
			.Select(entry => new { id = entry.Id, accountId = entry.AccountId, position = entry.Position, amount = entry.Amount, rate = entry.Rate })),
		Template template => JsonSerializer.SerializeToElement(template.Entries.OrderBy(entry => entry.Position)
			.Select(entry => new { id = entry.Id, accountId = entry.AccountId, position = entry.Position, amount = entry.Amount })),
		_ => throw new InvalidOperationException("Unsupported aggregate.")
	};

	private static void ReadEntries(BaseEntity entity, JsonElement entries)
	{
		HashSet<Guid> ids = new();
		int position = 0;
		foreach (JsonElement entry in entries.EnumerateArray())
		{
			string[] fields = entity is Transaction ? ["id", "accountId", "position", "amount", "rate"]
				: ["id", "accountId", "position", "amount"];
			if (!fields.ToHashSet(StringComparer.Ordinal).SetEquals(entry.EnumerateObject().Select(property => property.Name)) ||
				entry.EnumerateObject().Count() != fields.Length)
			{
				throw new InvalidSyncBatchException("An aggregate entry has incomplete or unsupported properties.");
			}
			Guid id = entry.GetProperty("id").GetGuid();
			Guid accountId = entry.GetProperty("accountId").GetGuid();
			if (id == Guid.Empty || accountId == Guid.Empty || !ids.Add(id) || entry.GetProperty("position").GetInt32() != position)
			{
				throw new InvalidSyncBatchException("Entry identities must be nonempty and unique; positions must be consecutive from zero.");
			}
			decimal amount = entry.GetProperty("amount").GetDecimal();
			if (entity is Transaction transaction)
			{
				transaction.Entries.Add(new TransactionEntry
				{
					Id = id, TransactionId = entity.Id, Transaction = transaction, AccountId = accountId,
					Account = null!, Amount = amount, Position = position, Rate = entry.GetProperty("rate").GetDecimal()
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
}
