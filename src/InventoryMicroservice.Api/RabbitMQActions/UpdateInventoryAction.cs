using InventoryMicroservice.Api.Interfaces;
using InventoryMicroservice.Common.Requests;
using InventoryMicroservice.Service.Interfaces;

namespace InventoryMicroservice.Api.RabbitMQActions;

public class UpdateInventoryAction: IRabbitMQAction<UpdateInventory>
{
  internal const string CommandKey = "update";

  private readonly IInventoryService _service;
  private readonly ILogger<UpdateInventoryAction> _logger;

  public UpdateInventoryAction(IInventoryService service, ILogger<UpdateInventoryAction> logger)
  {
    _service = service;
    _logger = logger;
  }

  public Task<bool> RunActionAsync(UpdateInventory input, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
