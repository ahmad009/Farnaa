using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Marketplace.Api;

internal sealed class OrderRequestExample : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.OperationId != "PlaceOrder" || operation.RequestBody is null) return;
        if (!operation.RequestBody.Content.TryGetValue("application/json", out var content)) return;

        content.Example = new OpenApiObject
        {
            ["customerId"] = new OpenApiString("40000000-0000-0000-0000-000000000001"),
            ["items"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["sellingOptionId"] = new OpenApiString("30000000-0000-0000-0000-000000000001"),
                    ["quantity"] = new OpenApiInteger(1)
                }
            }
        };
    }
}
