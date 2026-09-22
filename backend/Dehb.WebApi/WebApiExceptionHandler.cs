using Business.Models.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;

namespace Dehb.WebApi;

/// <summary>
/// Defines the Web API exception handler.
/// Logs unhandled exceptions and converts known business failures into HTTP problem details.
/// ASP.NET Core resolves the handler from dependency injection during exception processing.
/// It recognizes business exceptions without exposing concrete business implementations.
/// It does not throw business exceptions or implement service validation.
/// </summary>
internal class WebApiExceptionHandler : IExceptionHandler
{
	private readonly ILogService _logService;

	public WebApiExceptionHandler(ILogService logService) => _logService = logService;

	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		_logService.Error(exception);

		int statusCode = exception switch
		{
			GroupNotFoundException => StatusCodes.Status404NotFound,
			ElementNotFoundException => StatusCodes.Status404NotFound,
			CurrencyNotFoundException => StatusCodes.Status404NotFound,
			InvalidGroupException => StatusCodes.Status400BadRequest,
			InvalidElementException => StatusCodes.Status400BadRequest,

			_ => StatusCodes.Status500InternalServerError
		};

		httpContext.Response.StatusCode = statusCode;
		await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
		{
			Status = statusCode,
			Title = statusCode == StatusCodes.Status500InternalServerError
				? "An unexpected error occurred."
				: exception.Message
		}, cancellationToken);

		return true;
	}
}
