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

  public async Task CreateItemInventoryStatusAsync(int catalogItemId, int startingAmount, CancellationToken cancellationToken = default)
  {
    await _repository.CreateItemInventoryStatusAsync(catalogItemId, startingAmount, cancellationToken);
  }

  public async Task DeleteItemInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken = default)
  {
    await _repository.DeleteItemInventoryStatusAsync(catalogItemId, cancellationToken);
  }

  public async Task<IEnumerable<InventoryStatus>> GetAllItemStatusesAsync(CancellationToken cancellationToken = default)
  {
    IEnumerable<InventoryStatus> status = await _repository.GetAllItemStatusesAsync(cancellationToken);
    return status;
  }

  public async Task<InventoryStatus?> GetItemInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken = default)
  {
    InventoryStatus? status = await _repository.GetItemInventoryStatusAsync(catalogItemId, cancellationToken);
    return status;
  }

  public async Task<IEnumerable<InventoryStatus>> GetItemStatusesAsync(IEnumerable<int> catalogItemIds, CancellationToken cancellationToken = default)
  {
    IEnumerable<InventoryStatus> status = await _repository.GetItemStatusesAsync(catalogItemIds, cancellationToken);
    return status;
  }

  public async Task UpdateItemInventoryStatusAsync(int catalogItemId, int amount, CancellationToken cancellationToken = default)
  {
    await _repository.UpdateItemInventoryStatusAsync(catalogItemId, amount, cancellationToken);
  }
}
