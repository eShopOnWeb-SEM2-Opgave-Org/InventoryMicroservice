using InventoryMicroservice.Common.Models;

namespace InventoryMicroservice.Service.Interfaces;

public interface IInventoryService
{
  Task<IEnumerable<InventoryStatus>> GetAllItemStatusesAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<InventoryStatus>> GetItemStatusesAsync(IEnumerable<int> catalogItemIds, CancellationToken cancellationToken = default);
  Task<InventoryStatus?> GetItemInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken = default);

  Task CreateItemInventoryStatusAsync(int catalogItemId, int startingAmount, CancellationToken cancellationToken = default);
  Task UpdateItemInventoryStatusAsync(int catalogItemId, int amount, CancellationToken cancellationToken = default);
  Task DeleteItemInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken = default);
}
