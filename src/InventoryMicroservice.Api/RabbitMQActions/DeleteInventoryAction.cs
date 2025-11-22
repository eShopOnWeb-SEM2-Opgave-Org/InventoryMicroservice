using InventoryMicroservice.Api.Interfaces;
using InventoryMicroservice.Common.Requests;
using InventoryMicroservice.Service.Interfaces;

namespace InventoryMicroservice.Api.RabbitMQActions;

public class DeleteInventoryAction: IRabbitMQAction<DeleteInventory>
{
  internal const string CommandKey = "delete";

  private readonly IInventoryService _service;
  private readonly ILogger<DeleteInventoryAction> _logger;

  public DeleteInventoryAction(IInventoryService service, ILogger<DeleteInventoryAction> logger)
  {
    _service = service;
    _logger = logger;
  }

  public async Task<bool> RunActionAsync(DeleteInventory input, CancellationToken cancellationToken)
  {
    try
    {
      await _service.DeleteItemInventoryStatusAsync(input.CatalogItemId, cancellationToken);
      return true;
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "[Origin: {Class}.{Method}] Could not delete inventory due to internal error",
        nameof(DeleteInventoryAction),
        nameof(RunActionAsync)
      );

      return false;
    }
  }
}
