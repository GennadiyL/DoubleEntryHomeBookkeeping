using Business.Contracts.Params;
using Business.Contracts.Services;
using Business.Contracts.Utils.Ordering;
using Business.Models.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class AccountService : IAccountService
{
	private readonly ISharedContext _sharedContext;
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly IAccountRepository _repository;
	private readonly IAccountGroupRepository _groupRepository;

	public AccountService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_repository = unitOfWork.AccountRepo;
		_groupRepository = unitOfWork.AccountGroupRepo;
	}

	public async Task<Guid> Add(AccountParam param)
	{
		if (param is null || string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty || param.CurrencyId == Guid.Empty)
		{
			throw new InvalidElementException("An account name, group identifier, and currency identifier are required.");
		}

		AccountGroup? group = await _groupRepository.GetWithContentsByIdAsync(param.GroupId);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (group.Elements.Any(element => string.Equals(element.Name, param.Name, StringComparison.Ordinal)))
		{
			throw new InvalidElementException("An element with the same name already exists in this group.");
		}

		int maxOrder = group.Elements.Count == 0 ? 0 : group.Elements.Max(element => element.Order);
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidElementException("The group has no available element ordering position.");
		}

		(Currency currency, Category? category, Correspondent? correspondent, Project? project) = await ResolveReferences(param);
		DateTime now = _sharedContext.DateTimeService.UtcNow;
		Account element = new()
		{
			Id = Guid.NewGuid(),
			CurrencyId = currency.Id,
			Currency = currency,
			CategoryId = category?.Id,
			Category = category,
			CorrespondentId = correspondent?.Id,
			Correspondent = correspondent,
			ProjectId = project?.Id,
			Project = project,
			Name = param.Name,
			Description = param.Description,
			IsFavorite = param.IsFavorite,
			GroupId = group.Id,
			Group = group,
			Order = maxOrder + 1,
			Original = now,
			Current = now
		};
		_repository.Add(element);
		await _unitOfWork.SaveChangesAsync();
		return element.Id;
	}

	public async Task Update(Guid entityId, AccountParam param)
	{
		if (entityId == Guid.Empty || param is null ||
			string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty || param.CurrencyId == Guid.Empty)
		{
			throw new InvalidElementException("An account identifier, name, group identifier, and currency identifier are required.");
		}

		Account element = await GetActiveElement(entityId);
		if (param.GroupId != element.GroupId)
		{
			throw new InvalidElementException("Use MoveToAnotherGroup to change the group.");
		}

		AccountGroup? group = await _groupRepository.GetWithContentsByIdAsync(element.GroupId);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}
		if (group.Elements.Any(item => item.Id != element.Id &&
			string.Equals(item.Name, param.Name, StringComparison.Ordinal)))
		{
			throw new InvalidElementException("An element with the same name already exists in this group.");
		}

		if (element.CurrencyId != param.CurrencyId &&
			await _unitOfWork.TransactionEntryRepo.HasByAccountIdAsync(entityId))
		{
			throw new InvalidElementException("The currency of an account used in a transaction cannot be changed.");
		}

		(Currency currency, Category? category, Correspondent? correspondent, Project? project) = await ResolveReferences(param);
		element.CurrencyId = currency.Id;
		element.Currency = currency;
		element.CategoryId = category?.Id;
		element.Category = category;
		element.CorrespondentId = correspondent?.Id;
		element.Correspondent = correspondent;
		element.ProjectId = project?.Id;
		element.Project = project;
		element.Name = param.Name;
		element.Description = param.Description;
		element.IsFavorite = param.IsFavorite;
		element.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task Delete(Guid entityId)
	{
		Account element = await GetActiveElement(entityId);
		element.IsDeleted = true;
		element.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task SetOrder(Guid entityId, int order)
	{
		if (order < 1)
		{
			throw new InvalidElementException("A positive order is required.");
		}

		Account element = await GetActiveElement(entityId);
		AccountGroup? group = await _groupRepository.GetWithContentsByIdAsync(element.GroupId);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		List<Account> siblings = [.. group.Elements.Where(item => !item.IsDeleted)
			.OrderBy(item => item.Order).ThenBy(item => item.Id)];
		Account? target = siblings.SingleOrDefault(item => item.Id == entityId);
		if (target is null)
		{
			throw new ElementNotFoundException("The element is no longer an active member of this group.");
		}
		if (order > siblings.Count)
		{
			throw new InvalidElementException("The order exceeds the number of active elements in this group.");
		}

		Dictionary<Guid, int> originalOrders = siblings.ToDictionary(item => item.Id, item => item.Order);
		siblings.Reorder();
		siblings.SetOrder(target, order);
		List<Account> changed = [.. siblings.Where(item => item.Order != originalOrders[item.Id])];
		if (changed.Count == 0)
		{
			return;
		}

		DateTime now = _sharedContext.DateTimeService.UtcNow;
		foreach (Account sibling in changed)
		{
			sibling.Current = now;
			_repository.Update(sibling);
		}
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task SetFavoriteStatus(Guid entityId, bool isFavorite)
	{
		Account element = await GetActiveElement(entityId);
		if (element.IsFavorite == isFavorite)
		{
			return;
		}

		element.IsFavorite = isFavorite;
		element.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task MoveToAnotherGroup(Guid entityId, Guid toGroupId)
	{
		if (entityId == Guid.Empty || toGroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier and destination group identifier are required.");
		}

		Account element = await GetActiveElement(entityId);
		AccountGroup? destination = await _groupRepository.GetWithContentsByIdAsync(toGroupId);
		if (destination is null || destination.IsDeleted)
		{
			throw new GroupNotFoundException("The destination group does not exist or is deleted.");
		}
		if (element.GroupId == toGroupId)
		{
			return;
		}
		if (destination.Elements.Any(item => string.Equals(item.Name, element.Name, StringComparison.Ordinal)))
		{
			throw new InvalidElementException("An element with the same name already exists in the destination group.");
		}

		int maxOrder = destination.Elements.Count == 0 ? 0 : destination.Elements.Max(item => item.Order);
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidElementException("The destination group has no available element ordering position.");
		}

		element.GroupId = destination.Id;
		element.Group = destination;
		element.Order = maxOrder + 1;
		element.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task CombineElements(Guid toElementId, Guid fromElementId)
	{
		if (toElementId == Guid.Empty || fromElementId == Guid.Empty || toElementId == fromElementId)
		{
			throw new InvalidElementException("Two different element identifiers are required.");
		}

		Account source = await GetActiveElement(fromElementId);
		Account destination = await GetActiveElement(toElementId);
		if (source.CurrencyId != destination.CurrencyId)
		{
			throw new InvalidElementException("Only accounts with the same currency can be combined.");
		}

		ICollection<TransactionEntry> transactionEntries = await _unitOfWork.TransactionEntryRepo.GetByAccountIdAsync(fromElementId);
		ICollection<TemplateEntry> templateEntries = await _unitOfWork.TemplateEntryRepo.GetByAccountIdAsync(fromElementId);
		DateTime now = _sharedContext.DateTimeService.UtcNow;
		foreach (TransactionEntry entry in transactionEntries)
		{
			entry.AccountId = destination.Id;
			entry.Account = destination;
			_unitOfWork.TransactionEntryRepo.Update(entry);
		}
		foreach (TemplateEntry entry in templateEntries)
		{
			entry.AccountId = destination.Id;
			entry.Account = destination;
			_unitOfWork.TemplateEntryRepo.Update(entry);
		}

		source.IsDeleted = true;
		source.Current = now;
		_repository.Update(source);
		await _unitOfWork.SaveChangesAsync();
	}

	private async Task<Account> GetActiveElement(Guid entityId)
	{
		if (entityId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier is required.");
		}

		Account? element = await _repository.GetByIdAsync(entityId, CancellationToken.None);
		if (element is null || element.IsDeleted)
		{
			throw new ElementNotFoundException("The element does not exist or is deleted.");
		}
		return element;
	}

	private async Task<(Currency Currency, Category? Category, Correspondent? Correspondent, Project? Project)> ResolveReferences(AccountParam param)
	{
		Currency? currency = await _unitOfWork.CurrencyRepo.GetByIdAsync(param.CurrencyId, CancellationToken.None);
		if (currency is null || currency.IsDeleted)
		{
			throw new CurrencyNotFoundException("The currency does not exist or is deleted.");
		}

		Category? category = await GetOptionalReference(param.CategoryId, _unitOfWork.CategoryRepo);
		Correspondent? correspondent = await GetOptionalReference(param.CorrespondentId, _unitOfWork.CorrespondentRepo);
		Project? project = await GetOptionalReference(param.ProjectId, _unitOfWork.ProjectRepo);
		return (currency, category, correspondent, project);
	}

	private static async Task<T?> GetOptionalReference<T>(Guid? id, IRepository<T> repository)
		where T : class, ICatalogEntity
	{
		if (id is null)
		{
			return null;
		}
		if (id == Guid.Empty)
		{
			throw new InvalidElementException("An optional reference must be a nonempty identifier or null.");
		}

		T? entity = await repository.GetByIdAsync(id.Value, CancellationToken.None);
		if (entity is null || entity.IsDeleted)
		{
			throw new ElementNotFoundException("The referenced element does not exist or is deleted.");
		}
		return entity;
	}
}
