using InventoryMicroservice.Common.Models;
using InventoryMicroservice.Common.Requests;

namespace InventoryMicroservice.Caller.Interfaces;

public interface IInventoryMicroserviceCaller
{
  Task<InventoryStatus?> GetInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken);
  Task<IEnumerable<InventoryStatus>> GetMultipleInventoryStatusAsync(IEnumerable<int> catalogItemIds, CancellationToken cancellationToken);

  Task CreateInventoryStatusAsync(
    CreateInventory request,
    Func<CancellationToken, Task> success,
    Func<CancellationToken, Task>? failure,
    CancellationToken cancellationToken
  );
  Task UpdateInventoryStatusAsync(
    UpdateInventory request,
    Func<CancellationToken, Task> success,
    Func<CancellationToken, Task>? failure,
    CancellationToken cancellationToken
  );
  Task DeleteInventoryStatusAsync(
    DeleteInventory request,
    Func<CancellationToken, Task> success,
    Func<CancellationToken, Task>? failure,
    CancellationToken cancellationToken
  );
}
