using Business.Contracts.Services;
using Business.Contracts.Services.Currencies;
using Business.Contracts.Utils.Merging;
using Business.Contracts.Utils.Ordering;
using Business.Impl.Operations.Config;
using Business.Impl.Operations.Currency;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using Shared.Contracts;

namespace Business.Impl.Services;

/// <summary>
/// Maintains saved currencies and exposes offline currency choices.
/// Uses the currency operation for regional ISO metadata.
/// Creates each currency and its initial fallback rate in one commit.
/// Validates rates using the shared configuration operation.
/// Preserves currency identities and immutable ISO codes on editing.
/// Protects the base currency and currencies referenced by accounts.
/// Soft deletion preserves rate history and existing content flags.
/// Orders live currencies independently from their content changes.
/// </summary>
internal sealed class CurrencyService : ICurrencyService
{
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly IConfigOperation _configOperation;
	private readonly ICurrencyOperation _currencyOperation;

	public CurrencyService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork,
		IConfigOperation configOperation, ICurrencyOperation currencyOperation)
	{
		_unitOfWork = unitOfWork;
		_configOperation = configOperation;
		_currencyOperation = currencyOperation;
	}

	public async Task<Guid> Add(string code, decimal initialRate, CancellationToken cancellationToken = default)
	{
		CurrencyProfile profile = _currencyOperation.GetCurrencyData(code);
		ICollection<Currency> currencies = await _unitOfWork.CurrencyRepo.GetAll(cancellationToken);
		if (currencies.Any(currency => !currency.IsDeleted() && string.Equals(currency.Code, profile.Code, StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidCurrencyException("A currency with this ISO code already exists.");
		}
		SystemConfig config = await _configOperation.GetSystemConfig(cancellationToken);
		decimal rate = Math.Round(initialRate, config.RatePrecision, MidpointRounding.ToEven);
		if (rate <= 0 || rate > AppValues.MaxDecimal)
		{
			throw new InvalidCurrencyException("The initial rate must be positive after rounding and within the supported storage range.");
		}
		int maxOrder = currencies.Where(currency => !currency.IsDeleted()).Select(currency => currency.Order).DefaultIfEmpty(-1).Max();
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidCurrencyException("The currency catalog has no available ordering position.");
		}
		Currency currency = new()
		{
			Id = Guid.NewGuid(), Code = profile.Code, Name = profile.Code, Symbol = profile.Symbol, EnglishName = profile.EnglishName,
			Order = maxOrder + 1, IsFavorite = false,
			EditRevision = null, DeleteRevision = null, ModificationType = ModificationType.None
		};
		CurrencyRate initial = new()
		{
			Id = Guid.NewGuid(), CurrencyId = currency.Id, Currency = currency,
			Date = AppValues.InitialDate, Rate = rate,
			EditRevision = null, DeleteRevision = null, ModificationType = ModificationType.None
		};
		currency.Rates.Add(initial);
		_unitOfWork.CurrencyRepo.Add(currency);
		_unitOfWork.CurrencyRateRepo.Add(initial);
		await _unitOfWork.SaveChanges(cancellationToken);
		return currency.Id;
	}

	public async Task Update(Guid currencyId, CurrencyParam param, CancellationToken cancellationToken = default)
	{
		Currency currency = await GetActiveCurrency(currencyId, cancellationToken);
		ValidateMetadata(param);
		currency.Name = param.Name.Trim();
		currency.SetEditedContent();
		_unitOfWork.CurrencyRepo.Update(currency);
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task Delete(Guid currencyId, CancellationToken cancellationToken = default)
	{
		Currency currency = await GetActiveCurrency(currencyId, cancellationToken);
		SystemConfig config = await _configOperation.GetSystemConfig(cancellationToken);
		if (currency.Id == config.BaseCurrencyId)
		{
			throw new InvalidCurrencyException("The base currency cannot be deleted.");
		}
		if (await _unitOfWork.AccountRepo.HasByCurrencyId(currencyId, cancellationToken))
		{
			throw new InvalidCurrencyException("The currency is referenced by an account.");
		}
		ICollection<Currency> currencies = await _unitOfWork.CurrencyRepo.GetAll(cancellationToken);
		List<Currency> survivors = [.. currencies.Where(item => item.Id != currencyId && !item.IsDeleted())
			.OrderBy(item => item.Order).ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		currency.SetDeleted();
		_unitOfWork.CurrencyRepo.Update(currency);
		for (int index = 0; index < survivors.Count; index++)
		{
			Currency survivor = survivors[index];
			if (survivor.Order == index)
			{
				continue;
			}
			survivor.Order = index;
			survivor.SetEditedOrder();
			_unitOfWork.CurrencyRepo.Update(survivor);
		}
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task SetOrder(Guid entityId, int order, CancellationToken cancellationToken = default)
	{
		if (order < 0)
		{
			throw new InvalidCurrencyException("A non-negative currency order is required.");
		}
		await GetActiveCurrency(entityId, cancellationToken);
		ICollection<Currency> currencies = await _unitOfWork.CurrencyRepo.GetAll(cancellationToken);
		List<Currency> ordered = [.. currencies.Where(currency => !currency.IsDeleted())
			.OrderBy(currency => currency.Order).ThenBy(currency => currency.Id.ToString("D"), StringComparer.Ordinal)];
		Currency? target = ordered.SingleOrDefault(currency => currency.Id == entityId);
		if (target is null)
		{
			throw new CurrencyNotFoundException("The currency does not exist or is deleted.");
		}
		if (order >= ordered.Count)
		{
			throw new InvalidCurrencyException("The order exceeds the number of active currencies.");
		}
		Dictionary<Guid, int> originalOrders = ordered.ToDictionary(currency => currency.Id, currency => currency.Order);
		ordered.Reorder();
		ordered.SetOrder(target, order);
		List<Currency> changed = [.. ordered.Where(currency => currency.Order != originalOrders[currency.Id])];
		if (changed.Count == 0)
		{
			return;
		}
		foreach (Currency currency in changed)
		{
			currency.SetEditedOrder();
			_unitOfWork.CurrencyRepo.Update(currency);
		}
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task SetFavoriteStatus(Guid entityId, bool isFavorite, CancellationToken cancellationToken = default)
	{
		Currency currency = await GetActiveCurrency(entityId, cancellationToken);
		if (currency.IsFavorite == isFavorite)
		{
			return;
		}
		currency.IsFavorite = isFavorite;
		currency.SetEditedContent();
		_unitOfWork.CurrencyRepo.Update(currency);
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	public async Task<CurrencyInfo> GetById(Guid id, CancellationToken cancellationToken = default) =>
		GetInfo(await GetActiveCurrency(id, cancellationToken));

	public async Task<List<CurrencyInfo>> GetAllCurrencies(CancellationToken cancellationToken = default)
	{
		ICollection<Currency> currencies = await _unitOfWork.CurrencyRepo.GetAll(cancellationToken);
		return [.. currencies.Where(currency => !currency.IsDeleted()).OrderBy(currency => currency.Order)
			.ThenBy(currency => currency.Id.ToString("D"), StringComparer.Ordinal).Select(GetInfo)];
	}

	public Task<List<AvailableCurrencyInfo>> GetAvailableCurrencies(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		List<AvailableCurrencyInfo> result = [.. _currencyOperation.GetListOfAvailableCurrencyData()
			.Select(profile => new AvailableCurrencyInfo { Code = profile.Code, EnglishName = profile.EnglishName, Symbol = profile.Symbol })];
		return Task.FromResult(result);
	}

	private async Task<Currency> GetActiveCurrency(Guid currencyId, CancellationToken cancellationToken = default)
	{
		Currency? currency = await _unitOfWork.CurrencyRepo.GetById(currencyId, cancellationToken);
		if (currency is null || currency.IsDeleted())
		{
			throw new CurrencyNotFoundException("The currency does not exist or is deleted.");
		}
		return currency;
	}

	private static void ValidateMetadata(CurrencyParam param)
	{
		if (param is null || string.IsNullOrWhiteSpace(param.Name) || param.Name.Trim().Length > 6)
		{
			throw new InvalidCurrencyException("Currency Name must contain 1 to 6 characters.");
		}
	}

	private static CurrencyInfo GetInfo(Currency currency) => new()
	{
		Id = currency.Id, Code = currency.Code, Name = currency.Name, Symbol = currency.Symbol, EnglishName = currency.EnglishName,
		Order = currency.Order, IsFavorite = currency.IsFavorite
	};
}
