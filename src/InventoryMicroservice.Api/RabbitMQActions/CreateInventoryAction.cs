using InventoryMicroservice.Api.Interfaces;
using InventoryMicroservice.Common.Requests;
using InventoryMicroservice.Service.Interfaces;

namespace InventoryMicroservice.Api.RabbitMQActions;

public class CreateInventoryAction: IRabbitMQAction<CreateInventory>
{
  public const string CommandKey = "create";

  private readonly IInventoryService _service;
  private readonly ILogger<CreateInventoryAction> _logger;

  public CreateInventoryAction(IInventoryService service, ILogger<CreateInventoryAction> logger)
  {
    _service = service;
    _logger = logger;
  }

  public async Task<bool> RunActionAsync(CreateInventory input, CancellationToken cancellationToken)
  {
    try
    {
      await _service.CreateItemInventoryStatusAsync(input.CatalogItemId, input.StartingAmount, cancellationToken);
      return true;
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "[Origin: {ClassName}.{MethodName}] Could not create inventory status due to internal error",
        nameof(CreateInventoryAction),
        nameof(RunActionAsync)
      );

      return true;
    }
  }
}
