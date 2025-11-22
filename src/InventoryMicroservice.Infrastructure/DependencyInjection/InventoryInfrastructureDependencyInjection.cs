using InventoryMicroservice.Infrastructure.Interfaces;
using InventoryMicroservice.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryMicroservice.Infrastructure.DependencyInjection;

public static class InventoryInfrastructureDependencyInjection
{
  public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection @this, string connectionString, string databaseName)
  {
    @this.AddKeyedSingleton<string>(InventoryRepository.CONNECTION_STRING_KEY, connectionString);
    @this.AddKeyedSingleton<string>(InventoryRepository.DATABASE_NAME_KEY, databaseName);

    @this.AddScoped<IInventoryRepository, InventoryRepository>();
    @this.AddScoped<InventoryRepository>();

    return @this;
  }

  public static async Task<IServiceScope> SetupInventoryDatabase(this IServiceScope @this)
  {
    InventoryRepository repository = @this.ServiceProvider.GetRequiredService<InventoryRepository>();
    await repository.SetupDbAsync();

    return @this;
  }
}
