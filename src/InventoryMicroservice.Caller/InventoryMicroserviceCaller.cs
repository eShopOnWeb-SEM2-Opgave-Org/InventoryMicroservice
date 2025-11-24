using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InventoryMicroservice.Caller.Interfaces;
using InventoryMicroservice.Common.Models;
using InventoryMicroservice.Common.Requests;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryMicroservice.Caller;

internal class InventoryMicroserviceCaller : IInventoryMicroserviceCaller
{
  private readonly IHttpClientFactory _httpFactory;
  private readonly ILogger<InventoryMicroserviceCaller> _logger;

  private readonly IConnection _connection;
  private readonly IChannel _channel;

  private readonly Dictionary<string, Func<CancellationToken, Task>> _onSuccess = new();
  private readonly Dictionary<string, Func<CancellationToken, Task>> _onFailure = new();

  private readonly string _responseQueueName;

  public InventoryMicroserviceCaller(IHttpClientFactory httpFactory, IServiceProvider serviceProvider, ILogger<InventoryMicroserviceCaller> logger)
  {
    _httpFactory = httpFactory;
    _logger = logger;

    string rabbitMQHost = serviceProvider.GetRequiredKeyedService<string>("rabbitMQHost");
    string rabbitMQUser = serviceProvider.GetRequiredKeyedService<string>("rabbitMQUser");
    string rabbitMQPassword = serviceProvider.GetRequiredKeyedService<string>("rabbitMQPassword");

    IConnectionFactory rabbitFactory = new ConnectionFactory
    {
      HostName = rabbitMQHost,
      UserName = rabbitMQUser,
      Password = rabbitMQPassword
    };

    Task<IConnection> connectionTask = rabbitFactory.CreateConnectionAsync();
    connectionTask.Wait();
    _connection = connectionTask.Result;

    Task<IChannel> channelTask = _connection.CreateChannelAsync();
    channelTask.Wait();
    _channel = channelTask.Result;

    Task<QueueDeclareOk> queueDeclareTask = _channel.QueueDeclareAsync(durable: true, exclusive: true, autoDelete: false);
    queueDeclareTask.Wait();
    _responseQueueName = queueDeclareTask.Result;

    Task setupConsumerTask = SetupConsumerAsync(_channel);
    setupConsumerTask.Wait();
  }

  public async Task<InventoryStatus?> GetInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken)
  {
    try
    {
      using HttpClient client = _httpFactory.CreateClient("inventory-microservice-http-client");
      string url = $"api/inventory?catalogItemId={catalogItemId}";

      _logger.LogInformation("Using address: " + client.BaseAddress + url);
      HttpResponseMessage response = await client.GetAsync(url, cancellationToken);

      if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.NoContent))
      {
        var error = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogError(
          "InventoryMicroservice API responded with status code = {Code}, and message = \"{Message}\"",
          response.StatusCode.ToString(),
          error
        );

        throw new InvalidOperationException("InventoryMicroservice responded with an invalid status code");
      }

      InventoryStatus? content = await response.Content.ReadFromJsonAsync<InventoryStatus>();
      return content;
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "[Origin: {Class}.{Method}] Could not fetch inventory status due to internal error",
        nameof(InventoryMicroserviceCaller),
        nameof(GetInventoryStatusAsync)
      );

      throw e;
    }
  }

  public async Task<IEnumerable<InventoryStatus>> GetMultipleInventoryStatusAsync(IEnumerable<int> catalogItemIds, CancellationToken cancellationToken)
  {
    try
    {
      using HttpClient client = _httpFactory.CreateClient("inventory-microservice-http-client");
      string url = $"api/inventory/multiple?catalogItemIds=";

      if (!catalogItemIds.Any())
        return [];

      url = url + string.Join("catalogItemIds=", catalogItemIds);

      HttpResponseMessage response = await client.GetAsync(url, cancellationToken);

      if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.NoContent))
      {
        var error = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogError(
          "InventoryMicroservice API responded with status code = {Code}, and message = \"{Message}\"",
          response.StatusCode.ToString(),
          error
        );

        throw new InvalidOperationException("InventoryMicroservice responded with an invalid status code");
      }

      IEnumerable<InventoryStatus>? content = await response.Content.ReadFromJsonAsync<IEnumerable<InventoryStatus>>();
      return content ?? [];
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "[Origin: {Class}.{Method}] Could not fetch inventory status due to internal error",
        nameof(InventoryMicroserviceCaller),
        nameof(GetMultipleInventoryStatusAsync)
      );

      throw e;
    }
  }

  public async Task CreateInventoryStatusAsync(CreateInventory request, Func<CancellationToken, Task> success, Func<CancellationToken, Task>? failure, CancellationToken cancellationToken)
  {
    if (_channel is null)
      throw new InvalidOperationException("Cannot create inventory entry when no channel has been made");

    InventoryRequestWrapper wrapper = new InventoryRequestWrapper
    {
      ReturnQueueName = _responseQueueName,
      Body = JsonSerializer.Serialize(request)
    };

    string body = JsonSerializer.Serialize(wrapper);
    byte[] message = Encoding.UTF8.GetBytes(body);

    Guid responseId = Guid.NewGuid();

    BasicProperties props = new BasicProperties();
    props.Headers = new Dictionary<string, object?>();

    props.Headers.Add("command-key", "create");
    props.Headers.Add("response-action-id", responseId.ToString());

    _onSuccess.Add(responseId.ToString(), success);
    if (failure is not null)
      _onFailure.Add(responseId.ToString(), failure);

    _logger.LogInformation("Sending create request, with return id: {Guid}, and command key create", responseId.ToString());
    await _channel.BasicPublishAsync("inventory-exchange", "inventory.input.create", mandatory: true, props, message);
  }

  public async Task DeleteInventoryStatusAsync(DeleteInventory request, Func<CancellationToken, Task> success, Func<CancellationToken, Task>? failure, CancellationToken cancellationToken)
  {
    if (_channel is null)
      throw new InvalidOperationException("Cannot update inventory entry when no channel has been made");

    InventoryRequestWrapper wrapper = new InventoryRequestWrapper
    {
      ReturnQueueName = _responseQueueName,
      Body = JsonSerializer.Serialize(request)
    };

    string body = JsonSerializer.Serialize(wrapper);
    byte[] message = Encoding.UTF8.GetBytes(body);

    Guid responseId = Guid.NewGuid();

    BasicProperties props = new BasicProperties();
    props.Headers = new Dictionary<string, object?>();

    props.Headers.Add("command-key", "delete");
    props.Headers.Add("response-action-id", responseId.ToString());

    _onSuccess.Add(responseId.ToString(), success);
    if (failure is not null)
      _onFailure.Add(responseId.ToString(), failure);

    await _channel.BasicPublishAsync("inventory-exchange", "inventory.input.delete", mandatory: true, props, message);
  }

  public async Task UpdateInventoryStatusAsync(UpdateInventory request, Func<CancellationToken, Task> success, Func<CancellationToken, Task>? failure, CancellationToken cancellationToken)
  {
    if (_channel is null)
      throw new InvalidOperationException("Cannot update inventory entry when no channel has been made");

    InventoryRequestWrapper wrapper = new InventoryRequestWrapper
    {
      ReturnQueueName = _responseQueueName,
      Body = JsonSerializer.Serialize(request)
    };

    string body = JsonSerializer.Serialize(wrapper);
    byte[] message = Encoding.UTF8.GetBytes(body);

    Guid responseId = Guid.NewGuid();

    BasicProperties props = new BasicProperties();
    props.Headers = new Dictionary<string, object?>();

    props.Headers.Add("command-key", "update");
    props.Headers.Add("response-action-id", responseId.ToString());

    _onSuccess.Add(responseId.ToString(), success);
    if (failure is not null)
      _onFailure.Add(responseId.ToString(), failure);

    await _channel.BasicPublishAsync("inventory-exchange", "inventory.input.update", mandatory: true, props, message);
  }

  private async Task SetupConsumerAsync(IChannel channel)
  {
    AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);

    await channel.BasicQosAsync(0, 1, false);

    consumer.ReceivedAsync += async (sender, content) =>
    {
      if (content.BasicProperties.Headers?.TryGetValue("response-action-id", out object? responseId) is not true)
      {
        _logger.LogError("Did not receive a respone id");
        return;
      }

      if (responseId is not byte[] validResponseId)
      {
        _logger.LogError("malformed response id");
        return;
      }

      string guid = Encoding.UTF8.GetString(validResponseId);
      _logger.LogInformation("Received return with id {Id}", guid);

      string status = Encoding.UTF8.GetString(content.Body.ToArray());
      _logger.LogInformation("Return status was {Status}", status);
      if (status.ToLowerInvariant() is "true" && _onSuccess.TryGetValue(guid.ToString(), out Func<CancellationToken, Task>? success))
        await success(default);
      else if (_onFailure.TryGetValue(guid, out Func<CancellationToken, Task>? failure))
        await failure(default);

      await channel.BasicAckAsync(content.DeliveryTag, false);
    };

    _logger.LogInformation("Binding consumer to response queue {QueueName}", _responseQueueName);
    await channel.BasicConsumeAsync(_responseQueueName, autoAck: false, consumer: consumer);
  }

  ~InventoryMicroserviceCaller()
  {
    if (_channel is IChannel channel)
    {
      Task exitChannelTask = channel.CloseAsync();
      exitChannelTask.Wait();
    }
    if (_connection is IConnection connection)
    {
      Task exitConnectionTask = connection.CloseAsync();
      exitConnectionTask.Wait();
    }
  }
}
