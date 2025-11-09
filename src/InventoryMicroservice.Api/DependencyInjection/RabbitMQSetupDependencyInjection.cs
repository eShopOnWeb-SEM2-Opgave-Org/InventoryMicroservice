using InventoryMicroservice.Api.Interfaces;
using InventoryMicroservice.Api.Models;
using InventoryMicroservice.Api.RabbitMQ;

namespace InventoryMicroservice.Api.DependencyInjection;

public static class RabbitMQSetupDependencyInjection
{
  public static IServiceCollection AddRabbitMQEntrypoints(this IServiceCollection @this, string rabbitMQHostname, string rabbitMQPort)
  {
    @this.AddKeyedSingleton<string>(InventoryEntrypoint.HOSTNAME_KEY, rabbitMQHostname);
    @this.AddKeyedSingleton<string>(InventoryEntrypoint.PORT_KEY, rabbitMQPort);

    @this.AddSingleton<IRabbitMQEntrypoint, InventoryEntrypoint>();

    return @this;
  }

  public static IServiceCollection AddRabbitMQCredentials(this IServiceCollection @this, string username, string password)
  {
    @this.AddSingleton<RabbitMQCredentials>(new RabbitMQCredentials { Username = username, Password = password});

    return @this;
  }

  public static IServiceScope StartRabbitMQEntryPoints(this IServiceScope @this)
  {
    IRabbitMQEntrypoint? entrypoint = @this.ServiceProvider.GetService<IRabbitMQEntrypoint>();
    if (entrypoint is not IRabbitMQEntrypoint validEntrypoint)
      throw new InvalidOperationException("No rabbit mq entry point was registred");

    Task setupTask = validEntrypoint.SetupRabbitMQAsync();
    setupTask.Wait();

    return @this;
  }
}
