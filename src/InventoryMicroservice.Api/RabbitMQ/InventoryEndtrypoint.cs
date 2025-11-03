using RabbitMQ.Client;

namespace InventoryMicroservice.Api.RabbitMQ;

public class InventoryEntrypoint
{
  private IConnection? _connection;
  private IChannel? _channel;

  ~InventoryEntrypoint()
  {
    if (_channel is IChannel channel && channel.IsOpen)
    {
      Task closeTask = channel.CloseAsync();
      closeTask.Wait();
    }

    if (_connection is IConnection connection)
    {
      Task closeTask = connection.CloseAsync();
      closeTask.Wait();
    }
  }
}

