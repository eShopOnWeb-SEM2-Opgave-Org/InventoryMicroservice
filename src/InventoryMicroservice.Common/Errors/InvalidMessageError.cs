
namespace InventoryMicroservice.Common.Errors;

public class InvalidMessageError
{
  public required string Reason { get; init; }
  public required string Body { get; init; }
}
