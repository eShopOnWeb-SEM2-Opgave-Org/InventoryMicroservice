
namespace InventoryMicroservice.Api.Interfaces;

public interface IRabbitMQEntrypoint
{
  Task SetupRabbitMQAsync();
}
