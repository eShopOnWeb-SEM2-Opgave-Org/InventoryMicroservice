
namespace InventoryMicroservice.Api.Interfaces;

public interface IRabbitMQAction<TInput>
{
  Task<bool> RunActionAsync(TInput input, CancellationToken cancellationToken = default);
}
