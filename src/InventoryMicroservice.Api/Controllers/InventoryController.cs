using InventoryMicroservice.Service.Interfaces;
using InventoryMicroservice.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryMicroservice.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController: ControllerBase
{
  private readonly IInventoryService _service;
  private readonly ILogger<InventoryController> _logger;

  public InventoryController(IInventoryService service, ILogger<InventoryController> logger)
  {
    _service = service;
    _logger = logger;
  }

  [HttpGet]
  public async Task<ActionResult<InventoryStatus>> GetInventoryStatusAsync([FromQuery] int catalogItemId, CancellationToken cancellationToken = default)
  {
    try
    {
      InventoryStatus? status = await _service.GetItemInventoryStatusAsync(catalogItemId, cancellationToken);

      if (status is not InventoryStatus inventory)
        return NoContent();

      return Ok(status);
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "[Opearation: {Class}.{Method}] Could not fetch inventory status for Catalog Item = {CatalogItemId}",
        nameof(InventoryController),
        nameof(GetInventoryStatusAsync),
        catalogItemId
      );

      return Problem("Could not fetch inventory status due to internal error");
    }
  }

  [HttpGet("multiple")]
  public async Task<ActionResult<IEnumerable<InventoryStatus>>> GetMultipleInventoryStatusAsync([FromQuery] IEnumerable<int> catalogItemIds, CancellationToken cancellationToken = default)
  {
    try
    {
      IEnumerable<InventoryStatus> status = await _service.GetItemStatusesAsync(catalogItemIds, cancellationToken);

      if (!status.Any())
        return NoContent();

      return Ok(status);
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "[Opearation: {Class}.{Method}] Could not fetch inventory status for Catalog Items = {CatalogItemId}",
        nameof(InventoryController),
        nameof(GetInventoryStatusAsync),
        catalogItemIds
      );

      return Problem("Could not fetch inventory status due to internal error");
    }
  }

}
