
namespace InventoryMicroservice.Common.Requests;

public class InventoryRequestWrapper
{
  public string? ReturnQueueName { get; set; }

  public required string Body { get; set; }
}
