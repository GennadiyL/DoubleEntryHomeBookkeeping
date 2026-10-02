using Business.Contracts.Services.Configs;
using Business.Contracts.Utils.Merging;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using DataAccess.Contracts;

namespace Business.Impl.Operations.Config;

/// <summary>
/// Loads and validates the current dataset configuration singletons.
/// Produces detached combined settings for the public configuration read.
/// Provides System or Local configuration for internal business consumers.
/// Rejects missing, duplicate or invalid singleton data without creating defaults.
/// Each read obtains current values without retaining a cached snapshot.
/// Missing or deleted balancing accounts are exposed as an absent selection.
/// Returned System values are copied without changing the stored configuration.
/// User-facing configuration editing remains owned by Setup.
/// </summary>
internal sealed class ConfigOperation : IConfigOperation
{
	private readonly IAppUnitOfWork _unitOfWork;

	public ConfigOperation(IAppUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	public async Task<ConfigurationInfo> GetConfiguration(CancellationToken cancellationToken = default)
	{
		SystemConfig system = await GetSystemConfig(cancellationToken);
		LocalConfig local = await GetLocalConfig(cancellationToken);
		return new ConfigurationInfo
		{
			BaseCurrencyId = system.BaseCurrencyId,
			BalancingAccountId = system.BalancingAccountId,
			AmountPrecision = system.AmountPrecision,
			RatePrecision = system.RatePrecision,
			AccountNameOrder = local.AccountNameOrder,
			DefaultAccountNameSeparator = local.DefaultAccountNameSeparator,
			ConflictPriority = local.ConflictPriority,
			SyncTrigger = local.SyncTrigger
		};
	}

	public async Task<SystemConfig> GetSystemConfig(CancellationToken cancellationToken = default)
	{
		ICollection<SystemConfig> configurations = await _unitOfWork.SystemConfigRepo.GetAllAsync(cancellationToken);
		if (configurations.Count != 1)
		{
			throw new InvalidOperationException("The System configuration singleton is missing or invalid.");
		}
		SystemConfig config = configurations.Single();
		if (config.IsDeleted() || config.AmountPrecision is < 0 or > 4 || config.RatePrecision is < 0 or > 4 ||
			config.BaseCurrencyId == Guid.Empty)
		{
			throw new InvalidOperationException("The System configuration singleton is missing or invalid.");
		}
		Guid? balancingAccountId = config.BalancingAccountId;
		if (balancingAccountId.HasValue)
		{
			Account? account = await _unitOfWork.AccountRepo.GetByIdAsync(balancingAccountId.Value, cancellationToken);
			if (account is null || account.IsDeleted())
			{
				balancingAccountId = null;
			}
		}
		return new SystemConfig
		{
			Id = config.Id,
			MasterDatasetKey = config.MasterDatasetKey,
			BaseCurrencyId = config.BaseCurrencyId,
			BalancingAccountId = balancingAccountId,
			AmountPrecision = config.AmountPrecision,
			RatePrecision = config.RatePrecision,
			EditRevision = config.EditRevision,
			DeleteRevision = config.DeleteRevision,
			ModificationType = config.ModificationType
		};
	}

	public async Task<LocalConfig> GetLocalConfig(CancellationToken cancellationToken = default)
	{
		ICollection<LocalConfig> configurations = await _unitOfWork.LocalConfigRepo.GetAllAsync(cancellationToken);
		if (configurations.Count != 1)
		{
			throw new InvalidOperationException("The Local configuration singleton is missing or invalid.");
		}
		LocalConfig config = configurations.Single();
		if (string.IsNullOrWhiteSpace(config.DefaultAccountNameSeparator) ||
			!Enum.IsDefined(config.AccountNameOrder) || config.AccountNameOrder == AccountNameOrder.Undefined ||
			!Enum.IsDefined(config.ConflictPriority) || config.ConflictPriority == ConflictPriority.Undefined ||
			!Enum.IsDefined(config.SyncTrigger))
		{
			throw new InvalidOperationException("The Local configuration settings are invalid.");
		}
		return config;
	}
}
