using System.Text;
using InventoryMicroservice.Api.Interfaces;
using InventoryMicroservice.Api.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryMicroservice.Api.RabbitMQ;

public class InventoryEntrypoint: IRabbitMQEntrypoint
{
  internal const string HOSTNAME_KEY = "inventory-hostname";
  internal const string PORT_KEY = "inventory-posrt";

  private readonly string _rabbitMQHostname;
  private readonly int _rabbitMQPort;

  private readonly RabbitMQCredentials _credentials;

  private IConnection? _connection;
  private IChannel? _channel;

  private const string DEAD_LETTER_EXCHANGE_NAME = "inventory-dead-letter-exchange";

  public InventoryEntrypoint(IServiceProvider provider, RabbitMQCredentials credentials)
  {
    string hostname = provider.GetRequiredKeyedService<string>(HOSTNAME_KEY);
    string port = provider.GetRequiredKeyedService<string>(PORT_KEY);

    if (!int.TryParse(port, out int portNo))
      throw new InvalidOperationException("The provided port is not a valid number");

    _rabbitMQHostname = hostname;
    _rabbitMQPort = portNo;

    _credentials = credentials;
  }

  public async Task SetupRabbitMQAsync()
  {
    ConnectionFactory factory = new ConnectionFactory()
    {
      HostName = _rabbitMQHostname,
      Port = _rabbitMQPort,
      UserName = _credentials.Username,
      Password = _credentials.Password
    };

    _connection = await factory.CreateConnectionAsync();
    _channel = await _connection.CreateChannelAsync();

    await SetupDeadLetterExchangeAsync();

    string exchangeName = "inventory-exchange";
    await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: false, autoDelete: true);

    Dictionary<string, object?> queueArguments = new()
    {
      {"x-message-ttl", 60_000},
      {"x-dead-letter-exchange", DEAD_LETTER_EXCHANGE_NAME},
      {"x-dead-letter-routing-key", "inventory.unread.message"}
    };

    string queueName = 
    await _channel.QueueDeclareAsync("inventory-input-queue", durable: false, exclusive: false, autoDelete: true, arguments: queueArguments);

    AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
    consumer.ReceivedAsync += async (sender, content) =>
    {
      var body = Encoding.UTF8.GetString(content.Body.ToArray());
      Console.WriteLine(body);

      await Task.CompletedTask;
    };

    await _channel.BasicConsumeAsync("inventory-input-queue", autoAck: false, consumer: consumer);
  }

  private async Task SetupDeadLetterExchangeAsync()
  {
    if (_channel is not IChannel channel)
      throw new InvalidOperationException("You cannot create a dead letter exchange before opening a channel");

    await _channel.ExchangeDeclareAsync(DEAD_LETTER_EXCHANGE_NAME, ExchangeType.Topic, durable: false, autoDelete: true);
  }

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

