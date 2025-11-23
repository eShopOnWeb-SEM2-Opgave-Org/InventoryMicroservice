using InventoryMicroservice.Caller.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryMicroservice.Caller.DependencyInjection;

public static class InventoryMicroserviceDependencyInjection
{
  public static IServiceCollection AddInventoryMicroserviceRabbitMQ(this IServiceCollection @this, string rabbitMQHost, string rabbitMQUser, string rabbitMQPassword)
  {
    @this.AddKeyedSingleton<string>("rabbitMQHost", rabbitMQHost);
    @this.AddKeyedSingleton<string>("rabbitMQUser", rabbitMQUser);
    @this.AddKeyedSingleton<string>("rabbitMQPassword", rabbitMQPassword);

    return @this;
  }

  public static IServiceCollection AddInventoryMicroserviceCaller(this IServiceCollection @this, string baseAddress)
  {
    @this.AddHttpClient("inventory-microservice-http-client", client => {
      client.BaseAddress = new Uri(baseAddress);
    });

    @this.AddScoped<IInventoryMicroserviceCaller, InventoryMicroserviceCaller>();

    return @this;
  }
}
