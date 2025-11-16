

namespace InventoryMicroservice.Common.Models;

public class InventoryStatus
{
  public required int ItemId { get; set; }
  public required int CatalogItemId { get; set; }
  public required int ItemCount { get; set; }
}
