
namespace InventoryMicroservice.Common.Requests;

public class CreateInventory
{
  public required int CatalogItemId { get; set; }
  public required int StartingAmount { get; set; }
}
