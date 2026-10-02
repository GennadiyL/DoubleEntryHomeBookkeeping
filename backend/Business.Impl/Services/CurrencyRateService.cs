using Business.Contracts.Services;
using Business.Contracts.Services.Currencies;
using Business.Contracts.Utils.Merging;
using Business.Impl.Operations.Config;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using Shared.Contracts;

namespace Business.Impl.Services;

/// <summary>
/// Maintains dated currency rates and supplies editor rate lookups.
/// Uses currency and calendar date as the upsert identity.
/// Normalizes rates with the shared System configuration precision.
/// Protects the initial fallback row from range deletion.
/// Soft deletion preserves existing revisions and modification flags.
/// Base-currency values remain one and account lookups are read-only.
/// Each mutation saves its complete result in a single commit.
/// Stored transaction-entry rates are never rewritten by this service.
/// </summary>
internal sealed class CurrencyRateService : ICurrencyRateService
{
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly IConfigOperation _configOperation;

	public CurrencyRateService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork, IConfigOperation configOperation)
	{
		_unitOfWork = unitOfWork;
		_configOperation = configOperation;
	}

	public async Task<Guid> AddOrUpdate(CurrencyRateParam param, CancellationToken cancellationToken = default)
	{
		if (param is null || param.CurrencyId == Guid.Empty)
		{
			throw new InvalidCurrencyException("A currency identifier and rate values are required.");
		}
		if (param.Date != AppValues.InitialDate && param.Date < AppValues.MinDate)
		{
			throw new InvalidCurrencyException("An ordinary currency rate date must be on or after the minimum supported date.");
		}
		Currency currency = await GetActiveCurrency(param.CurrencyId, cancellationToken);
		SystemConfig config = await _configOperation.GetSystemConfig(cancellationToken);
		decimal rate = Math.Round(param.Rate, config.RatePrecision, MidpointRounding.ToEven);
		if (rate <= 0 || rate > AppValues.MaxDecimal)
		{
			throw new InvalidCurrencyException("The rate must be positive after rounding and within the supported storage range.");
		}
		if (currency.Id == config.BaseCurrencyId && rate != 1m)
		{
			throw new InvalidCurrencyException("The base-currency rate must be one.");
		}
		CurrencyRate? existing = await _unitOfWork.CurrencyRateRepo.GetByCurrencyAndDateAsync(currency.Id, param.Date, cancellationToken);
		if (existing is not null && existing.IsDeleted())
		{
			throw new InvalidCurrencyException("The rate for this currency and date has been deleted.");
		}
		if (existing is null)
		{
			existing = new CurrencyRate
			{
				Id = Guid.NewGuid(), CurrencyId = currency.Id, Currency = currency,
				Date = param.Date, Rate = rate, Description = param.Description,
				EditRevision = null, DeleteRevision = null, ModificationType = ModificationType.None
			};
			_unitOfWork.CurrencyRateRepo.Add(existing);
		}
		else
		{
			if (existing.Rate == rate && existing.Description == param.Description)
			{
				return existing.Id;
			}
			existing.Rate = rate;
			existing.Description = param.Description;
			existing.SetEditedContent();
			_unitOfWork.CurrencyRateRepo.Update(existing);
		}
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return existing.Id;
	}

	public async Task Delete(Guid currencyId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
	{
		if (fromDate > toDate)
		{
			throw new InvalidCurrencyException("The start date must not be after the end date.");
		}
		await GetActiveCurrency(currencyId, cancellationToken);
		ICollection<CurrencyRate> rates = await _unitOfWork.CurrencyRateRepo.GetByCurrencyAndDateRangeAsync(currencyId, fromDate, toDate, cancellationToken);
		List<CurrencyRate> selected = [.. rates.Where(rate => !rate.IsInitial && !rate.IsDeleted())];
		if (selected.Count == 0)
		{
			return;
		}
		foreach (CurrencyRate rate in selected)
		{
			rate.SetDeleted();
			_unitOfWork.CurrencyRateRepo.Update(rate);
		}
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task<List<CurrencyRateInfo>> GetRates(Guid currencyId, CancellationToken cancellationToken = default)
	{
		await GetActiveCurrency(currencyId, cancellationToken);
		ICollection<CurrencyRate> rates = await _unitOfWork.CurrencyRateRepo.GetByCurrencyIdAsync(currencyId, cancellationToken);
		return [.. rates.Where(rate => !rate.IsDeleted()).OrderByDescending(rate => rate.Date).Select(rate => new CurrencyRateInfo
		{
			CurrencyId = rate.CurrencyId, Date = rate.Date, Rate = rate.Rate,
			Description = rate.Description, IsInitial = rate.IsInitial
		})];
	}

	public async Task<decimal> GetRate(Guid accountId, DateOnly date, CancellationToken cancellationToken = default)
	{
		Account? account = await _unitOfWork.AccountRepo.GetByIdAsync(accountId, cancellationToken);
		if (account is null || account.IsDeleted())
		{
			throw new ElementNotFoundException("The account does not exist or is deleted.");
		}
		Currency currency = await GetActiveCurrency(account.CurrencyId, cancellationToken);
		SystemConfig config = await _configOperation.GetSystemConfig(cancellationToken);
		if (currency.Id == config.BaseCurrencyId)
		{
			return 1m;
		}
		CurrencyRate? applicable = await _unitOfWork.CurrencyRateRepo.GetApplicableAsync(currency.Id, date, cancellationToken);
		if (applicable is null)
		{
			throw new InvalidOperationException("The currency has no applicable rate.");
		}
		if (applicable.Rate <= 0 || applicable.Rate > AppValues.MaxDecimal)
		{
			throw new InvalidOperationException("The applicable currency rate is invalid.");
		}
		return applicable.Rate;
	}

	private async Task<Currency> GetActiveCurrency(Guid currencyId, CancellationToken cancellationToken = default)
	{
		Currency? currency = await _unitOfWork.CurrencyRepo.GetByIdAsync(currencyId, cancellationToken);
		if (currency is null || currency.IsDeleted())
		{
			throw new CurrencyNotFoundException("The currency does not exist or is deleted.");
		}
		return currency;
	}
}
