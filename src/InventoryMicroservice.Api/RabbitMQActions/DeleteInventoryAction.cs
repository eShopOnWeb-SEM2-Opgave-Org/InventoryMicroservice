using InventoryMicroservice.Api.Interfaces;
using InventoryMicroservice.Common.Requests;
using InventoryMicroservice.Service.Interfaces;

namespace InventoryMicroservice.Api.RabbitMQActions;

public class DeleteInventoryAction : IRabbitMQAction<DeleteInventory>
{
  internal static string CommandKey = "delete";

  private readonly IInventoryService _service;
  private readonly ILogger<DeleteInventoryAction> _logger;

  public DeleteInventoryAction(IInventoryService service, ILogger<DeleteInventoryAction> logger)
  {
    _service = service;
    _logger = logger;
  }

  public Task RunActionAsync(DeleteInventory input, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
