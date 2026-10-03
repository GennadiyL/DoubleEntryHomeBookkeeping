using Business.Impl.Operations.Config;
using Business.Contracts.Services;
using Business.Contracts.Services.Templates;
using Business.Contracts.Utils.Merging;
using Business.Contracts.Utils.Ordering;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using Shared.Contracts;

namespace Business.Impl.Services;

/// <summary>
/// Implements template editing and preparation of unsaved editor values.
/// Validates template names, groups, account references and amount precision.
/// Creates entries in submitted order and replaces the complete set on update.
/// Saves aggregate changes and synchronization tracking in one commit.
/// Soft deletion retains entries for the shared deletion lifecycle.
/// Catalog movement and ordering affect only the required tracking flags.
/// Applying a template resolves rates without creating a transaction.
/// Preparing from a transaction copies values without changing its source.
/// </summary>
internal sealed class TemplateService : ITemplateService
{
	private readonly IConfigOperation _configOperation;
	private readonly ISharedContext _sharedContext;
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly ITemplateRepository _repository;
	private readonly ITemplateGroupRepository _groupRepository;

	public TemplateService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork, IConfigOperation configOperation)
	{
		_configOperation = configOperation;
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_repository = unitOfWork.TemplateRepo;
		_groupRepository = unitOfWork.TemplateGroupRepo;
	}

	public async Task<Guid> Add(TemplateParam param, CancellationToken cancellationToken = default)
	{
		if (param is null || string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element name and group identifier are required.");
		}

		TemplateGroup? group = await _groupRepository.GetWithContentsById(param.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new InvalidElementException("The group does not exist or is deleted.");
		}

		if (group.Elements.Any(element => string.Equals(element.Name.Trim(), param.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidElementException("An element with the same name already exists in this group.");
		}

		int maxOrder = group.Elements.Where(element => !element.IsDeleted()).Select(element => element.Order).DefaultIfEmpty(-1).Max();
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidElementException("The group has no available element ordering position.");
		}

		List<TemplateEntry> entries = await PrepareEntries(param, cancellationToken);
		Template element = new()
		{
			Id = Guid.NewGuid(),
			Name = param.Name.Trim(),
			Description = param.Description,
			IsFavorite = param.IsFavorite,
			GroupId = group.Id,
			Group = group,
			Order = maxOrder + 1,
			EditRevision = null,
			DeleteRevision = null,
			ModificationType = ModificationType.None,
		};
		AssignEntries(element, entries);
		_repository.Add(element);
		foreach (TemplateEntry entry in entries)
		{
			_unitOfWork.TemplateEntryRepo.Add(entry);
		}
		await _unitOfWork.SaveChanges(cancellationToken);
		return element.Id;
	}

	public async Task Update(Guid entityId, TemplateParam param, CancellationToken cancellationToken = default)
	{
		if (entityId == Guid.Empty || param is null ||
			string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier, name, and group identifier are required.");
		}

		Template element = await GetActiveElement(entityId, cancellationToken);
		if (param.GroupId != element.GroupId)
		{
			throw new InvalidElementException("Use MoveToAnotherGroup to change the group.");
		}

		TemplateGroup? group = await _groupRepository.GetWithContentsById(element.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new InvalidElementException("The group does not exist or is deleted.");
		}
		if (group.Elements.Any(item => item.Id != element.Id &&
			string.Equals(item.Name.Trim(), param.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidElementException("An element with the same name already exists in this group.");
		}

		List<TemplateEntry> entries = await PrepareEntries(param, cancellationToken);
		ICollection<TemplateEntry> previous = await _unitOfWork.TemplateEntryRepo.GetByTemplateId(entityId, cancellationToken);
		element.Name = param.Name.Trim();
		element.Description = param.Description;
		element.IsFavorite = param.IsFavorite;
		AssignEntries(element, entries);
		_unitOfWork.TemplateEntryRepo.RemoveRange(previous);
		foreach (TemplateEntry entry in entries)
		{
			_unitOfWork.TemplateEntryRepo.Add(entry);
		}
		element.SetEditedContent();
		_repository.Update(element);
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task Delete(Guid entityId, CancellationToken cancellationToken = default)
	{
		Template element = await GetActiveElement(entityId, cancellationToken);
		TemplateGroup group = await GetActiveGroup(element.GroupId, cancellationToken);
		element.SetDeleted();
		_repository.Update(element);
		NormalizeElements(group.Elements.Where(item => item.Id != element.Id && !item.IsDeleted()));
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task SetOrder(Guid entityId, int order, CancellationToken cancellationToken = default)
	{
		if (order < 0)
		{
			throw new InvalidElementException("A non-negative order is required.");
		}

		Template element = await GetActiveElement(entityId, cancellationToken);
		TemplateGroup? group = await _groupRepository.GetWithContentsById(element.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		List<Template> siblings = [.. group.Elements.Where(item => !item.IsDeleted())
			.OrderBy(item => item.Order).ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		Template? target = siblings.SingleOrDefault(item => item.Id == entityId);
		if (target is null)
		{
			throw new ElementNotFoundException("The element is no longer an active member of this group.");
		}
		if (order >= siblings.Count)
		{
			throw new InvalidElementException("The order exceeds the number of active elements in this group.");
		}

		Dictionary<Guid, int> originalOrders = siblings.ToDictionary(item => item.Id, item => item.Order);
		siblings.Reorder();
		siblings.SetOrder(target, order);
		List<Template> changed = [.. siblings.Where(item => item.Order != originalOrders[item.Id])];
		if (changed.Count == 0)
		{
			return;
		}

		foreach (Template sibling in changed)
		{
			sibling.SetEditedOrder();
			_repository.Update(sibling);
		}
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task SetFavoriteStatus(Guid entityId, bool isFavorite, CancellationToken cancellationToken = default)
	{
		Template element = await GetActiveElement(entityId, cancellationToken);
		if (element.IsFavorite == isFavorite)
		{
			return;
		}

		element.IsFavorite = isFavorite;
		element.SetEditedContent();
		_repository.Update(element);
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task MoveToAnotherGroup(Guid entityId, Guid toGroupId, CancellationToken cancellationToken = default)
	{
		if (entityId == Guid.Empty || toGroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier and destination group identifier are required.");
		}

		Template element = await GetActiveElement(entityId, cancellationToken);
		TemplateGroup? destination = await _groupRepository.GetWithContentsById(toGroupId, cancellationToken);
		if (destination is null || destination.IsDeleted())
		{
			throw new InvalidElementException("The destination group does not exist or is deleted.");
		}
		if (element.GroupId == toGroupId)
		{
			return;
		}
		if (destination.Elements.Any(item =>
			string.Equals(item.Name.Trim(), element.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidElementException("An element with the same name already exists in the destination group.");
		}

		TemplateGroup source = await GetActiveGroup(element.GroupId, cancellationToken);
		List<Template> destinationElements = [.. destination.Elements.Where(item => !item.IsDeleted())
			.OrderBy(item => item.Order).ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		NormalizeElements(source.Elements.Where(item => item.Id != element.Id && !item.IsDeleted()));
		NormalizeElements(destinationElements);
		element.GroupId = destination.Id;
		element.Group = destination;
		element.Order = destinationElements.Count;
		element.SetEditedContent();
		element.SetEditedOrder();
		_repository.Update(element);
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public Task CombineElements(Guid toElementId, Guid fromElementId, CancellationToken cancellationToken = default) =>
		throw new NotSupportedException("Combining templates is not supported.");

	private async Task<TemplateGroup> GetActiveGroup(Guid groupId, CancellationToken cancellationToken = default)
	{
		TemplateGroup? group = await _groupRepository.GetWithContentsById(groupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}
		return group;
	}

	private void NormalizeElements(IEnumerable<Template> elements)
	{
		List<Template> ordered = [.. elements.OrderBy(item => item.Order)
			.ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		for (int index = 0; index < ordered.Count; index++)
		{
			Template item = ordered[index];
			if (item.Order == index)
			{
				continue;
			}
			item.Order = index;
			item.SetEditedOrder();
			_repository.Update(item);
		}
	}

	private async Task<Template> GetActiveElement(Guid entityId, CancellationToken cancellationToken = default)
	{
		if (entityId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier is required.");
		}

		Template? element = await _repository.GetById(entityId, cancellationToken);
		if (element is null || element.IsDeleted())
		{
			throw new ElementNotFoundException("The element does not exist or is deleted.");
		}
		return element;
	}

	private async Task<List<TemplateEntry>> PrepareEntries(TemplateParam param, CancellationToken cancellationToken = default)
	{
		SystemConfig config = await _configOperation.GetSystemConfig(cancellationToken);
		List<TemplateEntry> entries = [];
		foreach (TemplateEntryParam input in param.Entries)
		{
			if (input is null)
			{
				throw new InvalidElementException("Every template entry must contain an account and amount.");
			}
			Account account = await GetAccount(input.AccountId, cancellationToken);
			decimal amount = Math.Round(input.Amount, config.AmountPrecision, MidpointRounding.ToEven);
			if (amount < AppValues.MinDecimal|| amount > AppValues.MaxDecimal)
			{
				throw new InvalidElementException("The entry amount exceeds the supported storage range.");
			}
			entries.Add(new TemplateEntry
			{
				Id = Guid.NewGuid(),
				Template = null!,
				Account = account,
				AccountId = account.Id,
				Amount = amount,
				Position = entries.Count
			});
		}
		return entries;
	}

	private static void AssignEntries(Template template, List<TemplateEntry> entries)
	{
		template.Entries = entries;
		foreach (TemplateEntry entry in entries)
		{
			entry.TemplateId = template.Id;
			entry.Template = template;
		}
	}

	private async Task<Account> GetAccount(Guid accountId, CancellationToken cancellationToken = default)
	{
		if (accountId == Guid.Empty)
		{
			throw new InvalidElementException("An entry account identifier is required.");
		}
		Account? account = await _unitOfWork.AccountRepo.GetById(accountId, cancellationToken);
		if (account is null || account.IsDeleted())
		{
			throw new ElementNotFoundException("The entry account does not exist or is deleted.");
		}
		return account;
	}

	private async Task<TemplateEntryInfo> GetEntryInfo(Guid accountId, decimal amount, CancellationToken cancellationToken = default)
	{
		Account account = await GetAccount(accountId, cancellationToken);
		Currency? currency = await _unitOfWork.CurrencyRepo.GetById(account.CurrencyId, cancellationToken);
		if (currency is null || currency.IsDeleted())
		{
			throw new CurrencyNotFoundException("The entry currency does not exist or is deleted.");
		}
		return new TemplateEntryInfo
		{
			AccountId = account.Id,
			AccountName = account.Name,
			CurrencyId = currency.Id,
			CurrencyName = currency.Name,
			Amount = amount
		};
	}

	public async Task<TemplateInfo> GetById(Guid id, CancellationToken cancellationToken = default)
	{
		Template? template = await _repository.GetById(id, cancellationToken);
		if (template is null || template.IsDeleted())
		{
			throw new ElementNotFoundException("The template does not exist or is deleted.");
		}
		TemplateGroup group = await GetActiveGroup(template.GroupId, cancellationToken);
		ICollection<TemplateEntry> entries = await _unitOfWork.TemplateEntryRepo.GetByTemplateId(id, cancellationToken);
		TemplateInfo result = new()
		{
			Id = template.Id,
			GroupId = template.GroupId,
			GroupName = group.Name,
			Name = template.Name,
			Description = template.Description,
			Order = template.Order,
			IsFavorite = template.IsFavorite
		};
		foreach (TemplateEntry entry in entries.OrderBy(item => item.Position))
		{
			result.Entries.Add(await GetEntryInfo(entry.AccountId, entry.Amount, cancellationToken));
		}
		return result;
	}

	public async Task<ApplyTemplateInfo> ApplyTemplate(Guid templateId, CancellationToken cancellationToken = default)
	{
		Template template = await GetActiveElement(templateId, cancellationToken);
		SystemConfig config = await _configOperation.GetSystemConfig(cancellationToken);
		DateTime now = _sharedContext.DateTimeService.UtcNow;
		DateOnly date = DateOnly.FromDateTime(now.ToLocalTime());
		ICollection<TemplateEntry> entries = await _unitOfWork.TemplateEntryRepo.GetByTemplateId(templateId, cancellationToken);
		ApplyTemplateInfo result = new() { DateTime = now, Description = template.Description };
		foreach (TemplateEntry entry in entries.OrderBy(item => item.Position))
		{
			TemplateEntryInfo info = await GetEntryInfo(entry.AccountId, entry.Amount, cancellationToken);
			decimal rate = 1m;
			if (info.CurrencyId != config.BaseCurrencyId)
			{
				CurrencyRate? applicable = await _unitOfWork.CurrencyRateRepo.GetApplicable(info.CurrencyId, date, cancellationToken);
				if (applicable is null)
				{
					throw new InvalidOperationException("The currency has no applicable rate.");
				}
				rate = applicable.Rate;
				if (rate <= 0 || rate > AppValues.MaxDecimal)
				{
					throw new InvalidOperationException("The applicable currency rate is invalid.");
				}
			}
			result.Entries.Add(new ApplyTemplateEntryInfo
			{
				AccountId = info.AccountId,
				AccountName = info.AccountName,
				Amount = info.Amount,
				Rate = rate
			});
		}
		return result;
	}

	public async Task<FromTransactionInfo> FromTransaction(Guid transactionId, CancellationToken cancellationToken = default)
	{
		Transaction? transaction = await _unitOfWork.TransactionRepo.GetById(transactionId, cancellationToken);
		if (transaction is null || transaction.IsDeleted())
		{
			throw new ElementNotFoundException("The transaction does not exist or is deleted.");
		}
		if (transaction.State is not (TransactionState.Draft or TransactionState.Confirmed))
		{
			throw new InvalidElementException("Only Draft or Confirmed transactions can be copied to a template.");
		}
		ICollection<TransactionEntry> entries = await _unitOfWork.TransactionEntryRepo.GetByTransactionId(transactionId, cancellationToken);
		FromTransactionInfo result = new() { Description = transaction.Description };
		foreach (TransactionEntry entry in entries.OrderBy(item => item.Position))
		{
			result.Entries.Add(await GetEntryInfo(entry.AccountId, entry.Amount, cancellationToken));
		}
		return result;
	}
}
