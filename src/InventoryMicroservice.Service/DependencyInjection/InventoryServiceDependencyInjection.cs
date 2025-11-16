using InventoryMicroservice.Service.Interfaces;
using InventoryMicroservice.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryMicroservice.Service.DependencyInjection;

public static class InventoryServiceDependencyInjection
{
  public static IServiceCollection AddInventoryServices(this IServiceCollection @this)
  {
    @this.AddScoped<IInventoryService, InventoryService>();

    return @this;
  }
}
