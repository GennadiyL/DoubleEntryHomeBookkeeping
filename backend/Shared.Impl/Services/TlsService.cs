using Shared.Contracts;
using Shared.Contracts.Tls;

namespace Shared.Impl.Services;

/// <summary>
/// Defines the thread-local context service.
/// Provides access to correlation data for the current logical execution flow.
/// Consumers resolve ITlsService and read or replace the current TlsData value.
/// It supplies business-independent context state used by cross-cutting services.
/// It does not use the context to make business decisions.
/// </summary>
internal class TlsService : ITlsService
{
	private static readonly AsyncLocal<TlsData> TlsData = new();

	public string CorrelationId
	{
		get
		{
			CreateTlsData();
			return TlsData.Value!.CorrelationId;
		}
		set
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(value);
			CreateTlsData();
			TlsData.Value!.CorrelationId = value.Trim();
		}
	}

	private void CreateTlsData() => TlsData.Value ??= new TlsData();
}
