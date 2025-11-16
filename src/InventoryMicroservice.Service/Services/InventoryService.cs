using InventoryMicroservice.Common.Models;
using InventoryMicroservice.Infrastructure.Interfaces;
using InventoryMicroservice.Service.Interfaces;

namespace InventoryMicroservice.Service.Services;

internal class InventoryService : IInventoryService
{
  private readonly IInventoryRepository _repository;

  public InventoryService(IInventoryRepository repository)
  {
    _repository = repository;
  }

  public Task CreateItemInventoryStatusAsync(int catalogItemId, int startingAmount, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteItemInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IEnumerable<InventoryStatus>> GetAllItemStatusesAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<InventoryStatus?> GetItemInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IEnumerable<InventoryStatus>> GetItemStatusesAsync(IEnumerable<int> catalogItemIds, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task UpdateItemInventoryStatusAsync(int catalogItemId, int amount, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
