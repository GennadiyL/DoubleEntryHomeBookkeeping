using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;

namespace UnitTests.Shared;

/// <summary>
/// Provides deterministic HTTP responses for real Azure SDK clients.
/// Records request destinations, content and conditions for storage tests.
/// Runs entirely in memory without contacting a storage account.
/// Requests use test credentials only.
/// Each fixture owns and disposes its handler and HTTP client.
/// </summary>
internal sealed class BlobStorageTestHandler : HttpMessageHandler
{
	public ConcurrentQueue<(Uri Uri, HttpMethod Method, byte[] Content, string? IfNoneMatch, string? Authorization, string? LeaseId)> Requests { get; } = new();

	public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? AsyncResponseFactory { get; set; }

	public Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory { get; set; } = Success;

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var content = request.Content == null
			? []
			: await request.Content.ReadAsByteArrayAsync(cancellationToken);
		Requests.Enqueue((request.RequestUri!, request.Method, content,
			request.Headers.IfNoneMatch.FirstOrDefault()?.Tag, request.Headers.Authorization?.ToString(),
			request.Headers.TryGetValues("x-ms-lease-id", out var leaseIds) ? leaseIds.Single() : null));
		return AsyncResponseFactory == null
			? ResponseFactory(request)
			: await AsyncResponseFactory(request, cancellationToken);
	}

	public static HttpResponseMessage Success(HttpRequestMessage request)
	{
		var status = request.Method == HttpMethod.Put ? HttpStatusCode.Created
			: request.Method == HttpMethod.Delete ? HttpStatusCode.Accepted : HttpStatusCode.OK;
		var response = new HttpResponseMessage(status) { Content = new ByteArrayContent([1, 2, 3]) };
		response.Headers.ETag = new EntityTagHeaderValue("\"test\"");
		response.Headers.Add("x-ms-blob-type", "BlockBlob");
		response.Content.Headers.LastModified = DateTimeOffset.UnixEpoch;
		return response;
	}

	public static HttpResponseMessage Failure(int status, string? errorCode)
	{
		var response = new HttpResponseMessage((HttpStatusCode)status) { Content = new ByteArrayContent([]) };
		if (errorCode != null)
		{
			response.Headers.Add("x-ms-error-code", errorCode);
		}

		return response;
	}
}
