using Business.Models.Constants;
using Adapter.WinUi;
using Business.Impl;
using DataAccess.Contracts;
using DataAccess.EntityFramework.SqLite;
using Business.Models.Entities.Config;
using DataAccess.Core.Entities;
using Messaging.InProcessBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Impl;
using Shared.Contracts;

namespace Dehb.WinUi.Hosting;

internal sealed partial class BusinessHost : IDisposable
{
	private readonly ISharedContext _sharedContext;
	private readonly ServiceProvider _services;
	public BusinessHost(ISharedContext sharedContext)
	{
		_sharedContext = sharedContext;
		IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", false).Build();
		string directory = Path.GetFullPath(configuration[StorageValues.LocalDirectoryConfigurationKey]!, AppContext.BaseDirectory);
		string localPath = Path.Combine(directory, StorageValues.LocalFileName);
		if (!_sharedContext.FileService.Exists(localPath))
		{
			throw new FileNotFoundException("The configured Local database was not found.", localPath);
		}
		configuration[StorageValues.LocalDirectoryConfigurationKey] = directory;
		ServiceCollection services = new();
		services.AddLogging();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddSharedModule();
		services.AddAdapterWinUiModule();
		services.AddBusinessModule();
		services.AddLocalDataAccessSqLiteModule(configuration);
		services.AddMessagingInProcessBusModule();
		_services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
	}

	public T Call<TS, T>(Func<TS, Task<T>> operation) where TS : notnull
	{
		using IServiceScope scope = _services.CreateScope();
		SynchronizationContext? previous = SynchronizationContext.Current;
		try
		{
			SynchronizationContext.SetSynchronizationContext(null);
			return operation(scope.ServiceProvider.GetRequiredService<TS>()).GetAwaiter().GetResult();
		}
		finally { SynchronizationContext.SetSynchronizationContext(previous); }
	}

	public void Call<TS>(Func<TS, Task> operation) where TS : notnull => Call<TS, bool>(async service =>
	{
		await operation(service).ConfigureAwait(false);
		return true;
	});

	public SystemConfig ReadSettings() => Call<IAppUnitOfWork, SystemConfig>(async unit =>
	{
		SystemConfig settings = (await unit.SystemConfigRepo.GetAll().ConfigureAwait(false)).Single();
		if (settings.BaseCurrencyId == Guid.Empty || settings.AmountPrecision is < 0 or > AppValues.MaxPrecision || settings.RatePrecision is < 0 or > AppValues.MaxPrecision)
		{
			throw new InvalidDataException("Invalid database precision settings.");
		}
		return new SystemConfig
		{
			BaseCurrencyId = settings.BaseCurrencyId,
			CurrentRevision = settings.CurrentRevision,
			AmountPrecision = settings.AmountPrecision,
			RatePrecision = settings.RatePrecision
		};
	});
	public T Atomic<TS, T>(Func<TS, Task<T>> operation) where TS : notnull => Call<IServiceProvider, T>(async provider =>
	{
		IAppUnitOfWork unit = provider.GetRequiredService<IAppUnitOfWork>();
		await using IUnitOfWorkTransaction transaction = await unit.BeginTransaction().ConfigureAwait(false);
		try
		{
			T result = await operation(provider.GetRequiredService<TS>()).ConfigureAwait(false);
			await unit.CommitTransaction(transaction).ConfigureAwait(false);
			return result;
		}
		catch { await unit.RollbackTransaction(transaction).ConfigureAwait(false); throw; }
	});
	public void Dispose() => _services.Dispose();
}
