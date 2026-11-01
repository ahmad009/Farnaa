using System.Reflection;
using Catalog.Contracts;
using Catalog.Domain;
using Catalog.Infrastructure;
using Ordering.Application;
using Ordering.Domain;
using Ordering.Infrastructure;
using Xunit;

namespace Marketplace.IntegrationTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domains_are_independent_of_frameworks_and_other_modules()
    {
        foreach (var assembly in new[] { typeof(Product).Assembly, typeof(Order).Assembly })
        {
            Assert.All(assembly.GetReferencedAssemblies(), reference =>
                Assert.StartsWith("System", reference.Name!));
        }
    }

    [Fact]
    public void Cross_module_dependencies_use_only_contracts()
    {
        var orderingAssemblies = new[] { typeof(PlaceOrder).Assembly, typeof(Order).Assembly,
            typeof(OrderingModule).Assembly, typeof(Ordering.Contracts.IPlaceOrder).Assembly };
        foreach (var assembly in orderingAssemblies)
            Assert.All(assembly.GetReferencedAssemblies().Where(x => x.Name!.StartsWith("Catalog.", StringComparison.Ordinal)),
                reference => Assert.Equal(typeof(ICatalogInventory).Assembly.GetName().Name, reference.Name));

        var catalogAssemblies = new[] { typeof(Product).Assembly, typeof(ICatalogInventory).Assembly,
            typeof(Catalog.Application.InventoryService).Assembly, typeof(CatalogModule).Assembly };
        foreach (var assembly in catalogAssemblies)
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x => x.Name!.StartsWith("Ordering.", StringComparison.Ordinal));
    }
}
