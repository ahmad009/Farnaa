using Catalog.Infrastructure;
using Marketplace.Api;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using Ordering.Contracts;
using Ordering.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Marketplace Orders API",
        Version = "v1",
        Description = "ثبت سفارش با قیمت روز و کنترل موجودی فروشنده"
    });
    options.OperationFilter<OrderRequestExample>();
});

var catalogConnection = DatabaseSettings.Resolve(builder.Configuration.GetConnectionString("CatalogDb"));
var orderingConnection = DatabaseSettings.Resolve(builder.Configuration.GetConnectionString("OrderingDb"));
var catalogSettings = new SqlConnectionStringBuilder(catalogConnection);
var orderingSettings = new SqlConnectionStringBuilder(orderingConnection);
if (string.Equals(catalogSettings.DataSource, orderingSettings.DataSource, StringComparison.OrdinalIgnoreCase) &&
    string.Equals(catalogSettings.InitialCatalog, orderingSettings.InitialCatalog, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Catalog and Ordering must use separate databases.");
builder.Services.AddCatalog(catalogConnection);
builder.Services.AddOrdering(orderingConnection);

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "Marketplace Orders API v1"));
}
await app.Services.InitializeCatalogAsync();
await app.Services.InitializeOrderingAsync();

app.MapPost("/api/orders", async (PlaceOrderRequest request, IPlaceOrder service, CancellationToken cancellationToken) =>
{
    var order = await service.ExecuteAsync(request, cancellationToken);
    return Results.Json(order, statusCode: StatusCodes.Status201Created);
})
    .WithName("PlaceOrder")
    .WithTags("Orders")
    .WithSummary("ثبت سفارش")
    .WithDescription("موجودی اقلام بررسی و کسر می‌شه و سفارش با قیمت لحظهٔ خرید ثبت می‌شه. شناسه‌های نمونه مربوط به داده‌های اولیهٔ پروژه هستن.")
    .Produces<OrderResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict)
    .ProducesProblem(StatusCodes.Status500InternalServerError)
    .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

app.Run();

public partial class Program;
