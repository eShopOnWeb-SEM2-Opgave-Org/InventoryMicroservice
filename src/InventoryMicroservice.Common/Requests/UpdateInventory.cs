
namespace InventoryMicroservice.Common.Requests;

public class UpdateInventory
{
  public required int CatalogItemId { get; init; }
  public required int newAmount { get; init; }
}
