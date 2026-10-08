using Business.Impl.Operations.Cumulative;
using DataAccess.Core.Entities;
using Business.Impl.Operations.Config;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services;
using Business.Contracts.Utils.Merging;
using Business.Contracts.Utils.Ordering;
using Business.Models.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class AccountService : IAccountService
{
	private readonly IConfigOperation _configOperation;
	private readonly ICumulativeOperation _cumulativeOperation;
	private readonly ISharedContext _sharedContext;
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly IAccountRepository _repository;
	private readonly IAccountGroupRepository _groupRepository;

	public AccountService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork, IConfigOperation configOperation, ICumulativeOperation cumulativeOperation)
	{
		_configOperation = configOperation;
		_cumulativeOperation = cumulativeOperation;
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_repository = unitOfWork.AccountRepo;
		_groupRepository = unitOfWork.AccountGroupRepo;
	}

	public async Task<AccountInfo> GetById(Guid id, CancellationToken cancellationToken = default)
	{
		Account? account = await _repository.GetById(id, cancellationToken);
		if (account is null || account.IsDeleted())
		{
			throw new ElementNotFoundException("The account does not exist or is deleted.");
		}
		AccountGroup? group = await _groupRepository.GetById(account.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}
		Currency? currency = await _unitOfWork.CurrencyRepo.GetById(account.CurrencyId, cancellationToken);
		if (currency is null || currency.IsDeleted())
		{
			throw new CurrencyNotFoundException("The currency does not exist or is deleted.");
		}
		Category? category = await GetOptionalReference(account.CategoryId, _unitOfWork.CategoryRepo, cancellationToken);
		Correspondent? correspondent = await GetOptionalReference(account.CorrespondentId, _unitOfWork.CorrespondentRepo, cancellationToken);
		Project? project = await GetOptionalReference(account.ProjectId, _unitOfWork.ProjectRepo, cancellationToken);
		return new AccountInfo
		{
			Id = account.Id,
			GroupId = account.GroupId,
			GroupName = group.Name,
			Name = account.Name,
			Description = account.Description,
			Order = account.Order,
			IsFavorite = account.IsFavorite,
			CurrencyId = account.CurrencyId,
			CurrencyName = currency.Name,
			CategoryId = account.CategoryId,
			CategoryName = category?.Name,
			CorrespondentId = account.CorrespondentId,
			CorrespondentName = correspondent?.Name,
			ProjectId = account.ProjectId,
			ProjectName = project?.Name
		};
	}

	public async Task<Guid> Add(AccountParam param, CancellationToken cancellationToken = default)
	{
		if (param is null || string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty || param.CurrencyId == Guid.Empty)
		{
			throw new InvalidElementException("An account name, group identifier, and currency identifier are required.");
		}

		AccountGroup? group = await _groupRepository.GetWithContentsById(param.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new InvalidElementException("The group does not exist or is deleted.");
		}


		int maxOrder = group.Elements.Where(element => !element.IsDeleted()).Select(element => element.Order).DefaultIfEmpty(-1).Max();
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidElementException("The group has no available element ordering position.");
		}

		(Currency currency, Category? category, Correspondent? correspondent, Project? project) = await ResolveReferences(param, cancellationToken);
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
		_repository.Add(element);
		await _unitOfWork.SaveChanges(cancellationToken);
		return element.Id;
	}

	public async Task Update(Guid entityId, AccountParam param, CancellationToken cancellationToken = default)
	{
		if (entityId == Guid.Empty || param is null ||
			string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty || param.CurrencyId == Guid.Empty)
		{
			throw new InvalidElementException("An account identifier, name, group identifier, and currency identifier are required.");
		}

		Account element = await GetActiveElement(entityId, cancellationToken);
		if (param.GroupId != element.GroupId)
		{
			throw new InvalidElementException("Use MoveToAnotherGroup to change the group.");
		}

		AccountGroup? group = await _groupRepository.GetWithContentsById(element.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new InvalidElementException("The group does not exist or is deleted.");
		}

		if (element.CurrencyId != param.CurrencyId)
		{
			throw new InvalidElementException("The currency of a saved account cannot be changed.");
		}

		(Currency currency, Category? category, Correspondent? correspondent, Project? project) = await ResolveReferences(param, cancellationToken);
		element.CurrencyId = currency.Id;
		element.Currency = currency;
		element.CategoryId = category?.Id;
		element.Category = category;
		element.CorrespondentId = correspondent?.Id;
		element.Correspondent = correspondent;
		element.ProjectId = project?.Id;
		element.Project = project;
		element.Name = param.Name.Trim();
		element.Description = param.Description;
		element.IsFavorite = param.IsFavorite;
		element.SetEditedContent();
		_repository.Update(element);
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task Delete(Guid entityId, CancellationToken cancellationToken = default)
	{
		Account element = await GetActiveElement(entityId, cancellationToken);
		if (await _unitOfWork.TransactionEntryRepo.HasByAccountId(entityId, cancellationToken) ||
			(await _unitOfWork.TemplateEntryRepo.GetByAccountId(entityId, cancellationToken)).Count != 0)
		{
			throw new InvalidElementException("The account is referenced by a transaction or template.");
		}

		AccountGroup group = await GetActiveGroup(element.GroupId, cancellationToken);
		element.SetDeleted();
		_repository.Update(element);
		NormalizeAccounts(group.Elements.Where(item => item.Id != element.Id && !item.IsDeleted()));
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task SetOrder(Guid entityId, int order, CancellationToken cancellationToken = default)
	{
		if (order < 0)
		{
			throw new InvalidElementException("A non-negative order is required.");
		}

		Account element = await GetActiveElement(entityId, cancellationToken);
		AccountGroup? group = await _groupRepository.GetWithContentsById(element.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		List<Account> siblings = [.. group.Elements.Where(item => !item.IsDeleted())
			.OrderBy(item => item.Order).ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		Account? target = siblings.SingleOrDefault(item => item.Id == entityId);
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
		List<Account> changed = [.. siblings.Where(item => item.Order != originalOrders[item.Id])];
		if (changed.Count == 0)
		{
			return;
		}

		foreach (Account sibling in changed)
		{
			sibling.SetEditedOrder();
			_repository.Update(sibling);
		}
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task SetFavoriteStatus(Guid entityId, bool isFavorite, CancellationToken cancellationToken = default)
	{
		Account element = await GetActiveElement(entityId, cancellationToken);
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
			throw new InvalidElementException("An account identifier and destination group identifier are required.");
		}

		Account element = await GetActiveElement(entityId, cancellationToken);
		AccountGroup? destination = await _groupRepository.GetWithContentsById(toGroupId, cancellationToken);
		if (destination is null || destination.IsDeleted())
		{
			throw new InvalidElementException("The destination group does not exist or is deleted.");
		}
		if (element.GroupId == toGroupId)
		{
			return;
		}

		AccountGroup source = await GetActiveGroup(element.GroupId, cancellationToken);
		List<Account> destinationAccounts = [.. destination.Elements.Where(item => !item.IsDeleted())];
		NormalizeAccounts(source.Elements.Where(item => item.Id != element.Id && !item.IsDeleted()));
		NormalizeAccounts(destinationAccounts);
		element.GroupId = destination.Id;
		element.Group = destination;
		element.Order = destinationAccounts.Count;
		element.SetEditedContent();
		element.SetEditedOrder();
		_repository.Update(element);
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task CombineElements(Guid toElementId, Guid fromElementId, CancellationToken cancellationToken = default)
	{
		if (toElementId == fromElementId)
		{
			return;
		}
		if (toElementId == Guid.Empty || fromElementId == Guid.Empty)
		{
			throw new InvalidElementException("Two account identifiers are required.");
		}

		IUnitOfWorkTransaction transactionScope = await _unitOfWork.BeginTransaction(cancellationToken);
		try
		{
			Account source = await GetActiveElement(fromElementId, cancellationToken);
			Account destination = await GetActiveElement(toElementId, cancellationToken);
			if (source.CurrencyId != destination.CurrencyId)
			{
				throw new InvalidElementException("Only accounts with the same currency can be combined.");
			}
	
			AccountGroup group = await GetActiveGroup(source.GroupId, cancellationToken);
			ICollection<TransactionEntry> transactionEntries = await _unitOfWork.TransactionEntryRepo.GetByAccountId(fromElementId, cancellationToken);
			ICollection<TemplateEntry> templateEntries = await _unitOfWork.TemplateEntryRepo.GetByAccountId(fromElementId, cancellationToken);
			List<Transaction> transactions = [];
			foreach (Guid id in transactionEntries.Select(entry => entry.TransactionId).Distinct())
			{
				Transaction? transaction = await _unitOfWork.TransactionRepo.GetById(id, cancellationToken);
				if (transaction is null)
				{
					throw new InvalidOperationException("A referenced transaction is missing.");
				}
				transactions.Add(transaction);
			}
			List<Template> templates = [];
			foreach (Guid id in templateEntries.Select(entry => entry.TemplateId).Distinct())
			{
				Template? template = await _unitOfWork.TemplateRepo.GetById(id, cancellationToken);
				if (template is null)
				{
					throw new InvalidOperationException("A referenced template is missing.");
				}
				templates.Add(template);
			}
	
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
			foreach (Transaction transaction in transactions)
			{
				transaction.SetEditedContent();
				_unitOfWork.TransactionRepo.Update(transaction);
			}
			foreach (Template template in templates)
			{
				template.SetEditedContent();
				_unitOfWork.TemplateRepo.Update(template);
			}
	
			source.SetDeleted();
			_repository.Update(source);
			NormalizeAccounts(group.Elements.Where(item => item.Id != source.Id && !item.IsDeleted()));
			await _unitOfWork.SaveChanges(cancellationToken);
			await _cumulativeOperation.Recalculate(destination.Id, cancellationToken);
			await _unitOfWork.CommitTransaction(transactionScope, cancellationToken);
		}
		catch
		{
			await _unitOfWork.RollbackTransaction(transactionScope, CancellationToken.None);
			throw;
		}
	}

	private async Task<AccountGroup> GetActiveGroup(Guid groupId, CancellationToken cancellationToken = default)
	{
		AccountGroup? group = await _groupRepository.GetWithContentsById(groupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}
		return group;
	}

	private void NormalizeAccounts(IEnumerable<Account> accounts)
	{
		List<Account> ordered = [.. accounts.OrderBy(item => item.Order)
			.ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		for (int index = 0; index < ordered.Count; index++)
		{
			Account item = ordered[index];
			if (item.Order == index)
			{
				continue;
			}
			item.Order = index;
			item.SetEditedOrder();
			_repository.Update(item);
		}
	}

	private async Task<Account> GetActiveElement(Guid entityId, CancellationToken cancellationToken = default)
	{
		if (entityId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier is required.");
		}

		Account? element = await _repository.GetById(entityId, cancellationToken);
		if (element is null || element.IsDeleted())
		{
			throw new ElementNotFoundException("The element does not exist or is deleted.");
		}
		return element;
	}

	private async Task<(Currency Currency, Category? Category, Correspondent? Correspondent, Project? Project)> ResolveReferences(AccountParam param, CancellationToken cancellationToken = default)
	{
		Currency? currency = await _unitOfWork.CurrencyRepo.GetById(param.CurrencyId, cancellationToken);
		if (currency is null || currency.IsDeleted())
		{
			throw new CurrencyNotFoundException("The currency does not exist or is deleted.");
		}

		Category? category = await GetOptionalReference(param.CategoryId, _unitOfWork.CategoryRepo, cancellationToken);
		Correspondent? correspondent = await GetOptionalReference(param.CorrespondentId, _unitOfWork.CorrespondentRepo, cancellationToken);
		Project? project = await GetOptionalReference(param.ProjectId, _unitOfWork.ProjectRepo, cancellationToken);
		return (currency, category, correspondent, project);
	}

	private static async Task<T?> GetOptionalReference<T>(Guid? id, IRepository<T> repository, CancellationToken cancellationToken = default)
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

		T? entity = await repository.GetById(id.Value, cancellationToken);
		if (entity is null || entity.IsDeleted())
		{
			throw new ElementNotFoundException("The referenced element does not exist or is deleted.");
		}
		return entity;
	}

	public async Task<string> GetDefaultName(Guid? correspondentId, Guid? categoryId, Guid? projectId, Guid? currencyId, CancellationToken cancellationToken = default)
	{
		LocalConfig config = await _configOperation.GetLocalConfig(cancellationToken);

		Correspondent? correspondent = await GetOptionalReference(correspondentId, _unitOfWork.CorrespondentRepo, cancellationToken);
		Category? category = await GetOptionalReference(categoryId, _unitOfWork.CategoryRepo, cancellationToken);
		Project? project = await GetOptionalReference(projectId, _unitOfWork.ProjectRepo, cancellationToken);
		string[] names = config.AccountNameOrder switch
		{
			AccountNameOrder.CorrespondentCategoryProject => [correspondent?.Name ?? "", category?.Name ?? "", project?.Name ?? ""],
			AccountNameOrder.CorrespondentProjectCategory => [correspondent?.Name ?? "", project?.Name ?? "", category?.Name ?? ""],
			AccountNameOrder.CategoryCorrespondentProject => [category?.Name ?? "", correspondent?.Name ?? "", project?.Name ?? ""],
			AccountNameOrder.CategoryProjectCorrespondent => [category?.Name ?? "", project?.Name ?? "", correspondent?.Name ?? ""],
			AccountNameOrder.ProjectCorrespondentCategory => [project?.Name ?? "", correspondent?.Name ?? "", category?.Name ?? ""],
			AccountNameOrder.ProjectCategoryCorrespondent => [project?.Name ?? "", category?.Name ?? "", correspondent?.Name ?? ""],
			_ => throw new InvalidOperationException("The account name order is invalid.")
		};
		string name = string.Join(config.AccountNameSeparator, names);
		if (!config.AccountNameAddCurrency)
		{
			return name;
		}
		if (!currencyId.HasValue || currencyId.Value == Guid.Empty)
		{
			throw new InvalidCurrencyException("A currency is required when the account name includes currency.");
		}
		Currency? currency = await _unitOfWork.CurrencyRepo.GetById(currencyId.Value, cancellationToken);
		if (currency is null || currency.IsDeleted())
		{
			throw new CurrencyNotFoundException("The currency does not exist or is deleted.");
		}
		return $"{name}({currency.Code})";
	}

}
