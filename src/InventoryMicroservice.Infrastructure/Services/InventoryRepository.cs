using System.Data;
using InventoryMicroservice.Common.Models;
using InventoryMicroservice.Infrastructure.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryMicroservice.Infrastructure.Services;

internal class InventoryRepository : IInventoryRepository
{
  internal const string CONNECTION_STRING_KEY = "inventory-connection-string";
  internal const string DATABASE_NAME_KEY = "inventory-database-name";

  private readonly string _connectionString;
  private readonly string _databaseName;
  private readonly ILogger<InventoryRepository> _logger;

  public InventoryRepository(IServiceProvider provider, ILogger<InventoryRepository> logger)
  {
    string? connectionString = provider.GetKeyedService<string>(CONNECTION_STRING_KEY);
    string? databaseName = provider.GetKeyedService<string>(DATABASE_NAME_KEY);

    if (connectionString is null)
      throw new InvalidOperationException("Could not create InventoryRepository due to missing connection string");
    if (databaseName is null)
      throw new InvalidOperationException("Could not create InventoryRepository due to missing database name");

    _connectionString = connectionString;
    _databaseName = databaseName;

    _logger = logger;
  }

  public async Task SetupDbAsync(CancellationToken cancellationToken = default)
  {
    string ensureDatabase = @"

";

    try
    {
      await using SqlConnection connection = new SqlConnection(_connectionString);
      if (connection.State is not ConnectionState.Closed)
        await connection.OpenAsync(cancellationToken);

      using SqlCommand createDb = connection.CreateCommand();
      createDb.CommandText = ensureDatabase;

      await createDb.ExecuteNonQueryAsync(cancellationToken);
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "Could not setup initial database inventory"
      );
    }
  }

  public Task CreateItemInventoryStatusAsync(int itemId, int startingAmount, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task DeleteItemInventoryStatusAsync(int itemId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IEnumerable<InventoryStatus>> GetAllItemStatusesAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<InventoryStatus> GetItemInventoryStatusAsync(int itemId, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task<IEnumerable<InventoryStatus>> GetItemStatusesAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public Task UpdateItemInventoryStatusAsync(int itemId, int amount, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }
}
