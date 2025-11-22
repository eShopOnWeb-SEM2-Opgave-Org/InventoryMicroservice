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

  public async Task<bool> RunActionAsync(UpdateInventory input, CancellationToken cancellationToken)
  {
    try
    {
      await _service.UpdateItemInventoryStatusAsync(input.CatalogItemId, input.NewAmount, cancellationToken);

      return true;
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "[Origin: {Class}.{Method}] Failed to update inventory status due to internal error",
        nameof(UpdateInventoryAction),
        nameof(RunActionAsync)
      );

      return false;
    }
  }
}
