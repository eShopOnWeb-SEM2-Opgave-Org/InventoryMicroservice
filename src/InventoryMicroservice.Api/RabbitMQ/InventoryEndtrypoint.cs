using System.Text;
using System.Text.Json;
using InventoryMicroservice.Api.Interfaces;
using InventoryMicroservice.Api.Models;
using InventoryMicroservice.Api.RabbitMQActions;
using InventoryMicroservice.Common.Errors;
using InventoryMicroservice.Common.Requests;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryMicroservice.Api.RabbitMQ;

public class InventoryEntrypoint: IRabbitMQEntrypoint
{
  internal const string HOSTNAME_KEY = "inventory-hostname";
  internal const string PORT_KEY = "inventory-posrt";

  public ILogger<InventoryEntrypoint> _logger;

  private readonly string _rabbitMQHostname;
  private readonly int _rabbitMQPort;

  private readonly IServiceProvider _serviceProvider;

  private readonly RabbitMQCredentials _credentials;

  private IConnection? _connection;
  private IChannel? _channel;

  private const string DEAD_LETTER_EXCHANGE_NAME = "inventory-dead-letter-exchange";
  private const string INVALID_MESSAGE_EXCHANGE_NAME = "inventory-invalid-message-exchange";

  public InventoryEntrypoint(IServiceProvider provider, RabbitMQCredentials credentials, IServiceProvider serviceProvider, ILogger<InventoryEntrypoint> logger)
  {
    string hostname = provider.GetRequiredKeyedService<string>(HOSTNAME_KEY);
    string port = provider.GetRequiredKeyedService<string>(PORT_KEY);

    if (!int.TryParse(port, out int portNo))
      throw new InvalidOperationException("The provided port is not a valid number");

    _rabbitMQHostname = hostname;
    _rabbitMQPort = portNo;

    _credentials = credentials;
    _serviceProvider = serviceProvider;
    _logger = logger;
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
    await SetupInvalidMessageExchangeAsync();

    string exchangeName = "inventory-exchange";
    await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: false, autoDelete: true);

    Dictionary<string, object?> queueArguments = new()
    {
      {"x-message-ttl", 60_000},
      {"x-dead-letter-exchange", DEAD_LETTER_EXCHANGE_NAME},
      {"x-dead-letter-routing-key", "inventory.unread.message"}
    };

    string queueName =
      await _channel.QueueDeclareAsync("inventory-input-queue", durable: true, exclusive: false, autoDelete: false, arguments: queueArguments);

    await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

    AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
    consumer.ReceivedAsync += async (sender, content) =>
    {
      if (content.BasicProperties?.Headers?.TryGetValue("command-key", out object? commandKey) is not true)
      {
        await SendMessageToInvalidAsync(content, "missing-command-key", "Message was missing command key");
        return;
      }
      if (commandKey is not byte[] byteCommandKey)
      {
        await SendMessageToInvalidAsync(content, "invalid-command-key", "Command key is not a valid string");
        return;
      }

      string strCommandKey = Encoding.UTF8.GetString(byteCommandKey);
      _logger.LogInformation("Command key is: " + strCommandKey);

      string body = Encoding.UTF8.GetString(content.Body.ToArray());
      InventoryRequestWrapper? wrapper = JsonSerializer.Deserialize<InventoryRequestWrapper>(body);

      if (wrapper is not InventoryRequestWrapper request)
      {
        await SendMessageToInvalidAsync(content, "missing-command-body", "Message was missing request body");
        return;
      }

      bool succesfulAction;
      try
      {
        succesfulAction = strCommandKey switch
        {
          UpdateInventoryAction.CommandKey => await HandleActionAsync<UpdateInventory>(wrapper.Body, strCommandKey),
          CreateInventoryAction.CommandKey => await HandleActionAsync<CreateInventory>(wrapper.Body, strCommandKey),
          DeleteInventoryAction.CommandKey => await HandleActionAsync<DeleteInventory>(wrapper.Body, strCommandKey),
          _ => throw new InvalidOperationException("Unknown command key")
        };
      }
      catch (Exception e)
      {
        await SendMessageToInvalidAsync(content, "command", e.Message);
        return;
      }

      if (wrapper.ReturnQueueName is string returnQueueName)
      {
        BasicProperties props = new BasicProperties(content.BasicProperties);

        await _channel.BasicPublishAsync(
          exchangeName,
          returnQueueName,
          mandatory: false,
          props,
          body: Encoding.UTF8.GetBytes(succesfulAction.ToString())
        );
      }

      await _channel.BasicAckAsync(content.DeliveryTag, multiple: false);
    };

    await _channel.BasicConsumeAsync("inventory-input-queue", autoAck: false, consumer: consumer);
  }

  private async Task<bool> HandleActionAsync<TInput>(string body, string commandKey)
  {
    TInput? input = JsonSerializer.Deserialize<TInput>(body);
    if (input is null)
      throw new InvalidOperationException("Cannot run command due to missing body");

    IRabbitMQAction<TInput>? action =
      _serviceProvider.GetRequiredKeyedService<IRabbitMQAction<TInput>>(commandKey);

    if (action is null)
      throw new InvalidOperationException("No command registerd for key: " + commandKey);

    bool result = await action.RunActionAsync(input);
    return result;
  }

  private async Task SetupDeadLetterExchangeAsync()
  {
    if (_channel is not IChannel channel)
      throw new InvalidOperationException("You cannot create a dead letter exchange before opening a channel");

    await _channel.ExchangeDeclareAsync(DEAD_LETTER_EXCHANGE_NAME, ExchangeType.Topic, durable: true, autoDelete: false);
    await _channel.QueueDeclareAsync("inventory.dead-letter", durable: true, exclusive: false, autoDelete: false);
    await _channel.QueueBindAsync("inventory.dead-letter", DEAD_LETTER_EXCHANGE_NAME, "inventory.unread.message");
  }

  private async Task SetupInvalidMessageExchangeAsync()
  {
    if (_channel is not IChannel channel)
      throw new InvalidOperationException("You cannot create an invalid message exchange before opening a channel");

    await _channel.ExchangeDeclareAsync(INVALID_MESSAGE_EXCHANGE_NAME, ExchangeType.Topic, durable: true, autoDelete: false);
    await _channel.QueueDeclareAsync("inventory.invalid", durable: true, exclusive: false, autoDelete: false);
    await _channel.QueueBindAsync("inventory.invalid", INVALID_MESSAGE_EXCHANGE_NAME, "inventory.invalid.#");
  }

  private async Task SendMessageToInvalidAsync(BasicDeliverEventArgs content, string routeSufix, string reason)
  {
    if (_channel is null)
      throw new InvalidOperationException("Cannot send message when no channel has been made");

    string body = Encoding.UTF8.GetString(content.Body.ToArray());
    InvalidMessageError error = new InvalidMessageError()
    {
      Reason = reason,
      Body = body
    };

    string errorBody = JsonSerializer.Serialize(error);
    byte[] invalid = Encoding.UTF8.GetBytes(errorBody);

    BasicProperties props = new BasicProperties();
    if (content.BasicProperties is not null)
      props = new BasicProperties(content.BasicProperties);

    await _channel.BasicPublishAsync(
      INVALID_MESSAGE_EXCHANGE_NAME,
      "inventory.invalid." + routeSufix,
      false,
      props,
      invalid
    );

    await _channel.BasicAckAsync(content.DeliveryTag, multiple: false);
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

