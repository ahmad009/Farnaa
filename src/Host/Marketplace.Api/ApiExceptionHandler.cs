using Catalog.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ordering.Contracts;

namespace Marketplace.Api;

internal sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            OrderValidationException e => (400, "Invalid order", e.Message),
            InventoryException { Code: "selling_option_not_found" } e => (404, e.Code, e.Message),
            InventoryException e => (409, e.Code, e.Message),
            SqlException { Number: -2 or 1205 or 1222 } => (503, "Database busy", "Retry the request later."),
            DbUpdateException { InnerException: SqlException { Number: -2 or 1205 or 1222 } } =>
                (503, "Database busy", "Retry the request later."),
            _ => (500, "Order processing failed", "The order could not be completed.")
        };
        if (status >= 500) logger.LogError(exception, "Order request failed. Trace {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        if (status == 503) context.Response.Headers.RetryAfter = "1";
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status, Title = title, Detail = detail,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
    }
}
