using InventoryMicroservice.Common.Models;

namespace InventoryMicroservice.Infrastructure.Interfaces;

public interface IInventoryRepository
{
  Task<IEnumerable<InventoryStatus>> GetAllItemStatusesAsync(CancellationToken cancellationToken = default);
  Task<IEnumerable<InventoryStatus>> GetItemStatusesAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken = default);
  Task<InventoryStatus> GetItemInventoryStatusAsync(int itemId, CancellationToken cancellationToken = default);

  Task CreateItemInventoryStatusAsync(int itemId, int startingAmount, CancellationToken cancellationToken = default);
  Task UpdateItemInventoryStatusAsync(int itemId, int amount, CancellationToken cancellationToken = default);
  Task DeleteItemInventoryStatusAsync(int itemId, CancellationToken cancellationToken = default);
}
