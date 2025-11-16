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
    string ensureDatabase = $@"
IF (DB_ID('{_databaseName}') IS NOT NULL)
THEN
  PRINT 'Database ""{_databaseName}"" already exists';
ELSE
  CREATE DATABASE [{_databaseName}];
END;
";

  string ensureData = $@"
DECLARE @insertItems INT = 1;
DECLARE @insertInventory INT = 1;

USE [{_databaseName}];

IF (OBJECT_ID('Items') IS NOT NULL)
THEN
  PRINT 'Items table already exists';
  SET @insertItems = 0;
ELSE
  CREATE TABLE [Items] (
    [ItemId] INT PRIMARY KEY NOT NULL IDENTITY(1,1),
    [CatalogItemId] INT NOT NULL
  );
END;

IF (@insertItems = 1)
THEN
  INSERT INTO [Items] (CatalogItemId)
  VALUES (1),
         (2),
         (3),
         (4),
         (5),
         (6),
         (7),
         (8),
         (9),
         (10),
         (12)
END;

IF (OBJECT_ID('Inventory') IS NOT NULL)
THEN
  PRINT 'Inventory table already exists';
  SET @insertInveotry = 0;
ELSE
  CREATE TABLE [Inventory] (
    InventoryId INT NOT NULL PRIMARY KEY IDNETITY(1, 1),
    ItemId INT NOT NULL FOREIGN KEY REFERENCES Items(ItemId),
    ItemCount INT NOT NULL
  );
END;

IF (@insertInveotry = 1)
THEN
  INSERT INTO [Inventory](ItemId, ItemCount)
  VALUES (),
END;
";

    try
    {
      await using SqlConnection connection = new SqlConnection(_connectionString);
      if (connection.State is not ConnectionState.Closed)
        await connection.OpenAsync(cancellationToken);

      using SqlCommand createDb = connection.CreateCommand();
      createDb.CommandText = ensureDatabase;

      await createDb.ExecuteNonQueryAsync(cancellationToken);

      using SqlCommand createData = connection.CreateCommand();
      createData.CommandText = ensureData;

      await createData.ExecuteNonQueryAsync(cancellationToken);
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "Could not setup initial database inventory"
      );

      throw e;
    }
  }

  public async Task CreateItemInventoryStatusAsync(int catalogItemId, int startingAmount, CancellationToken cancellationToken = default)
  {
    string createItem = $@"
USE [{_databaseName}];

INSERT INTO [Item](CatalogItemId)
OUTPUT INSERTED.ItemId
VALUES(@{nameof(catalogItemId)});
";

    string createStatus = $@"
USE [{_databaseName}];

INSERT INTO [Inventory] (ItemId, ItemCount)
VALUES (itemId, @{nameof(startingAmount)});
";

    try
    {
      await using SqlConnection connection = new SqlConnection(_connectionString);
      if (connection.State is ConnectionState.Closed)
        await connection.OpenAsync(cancellationToken);
      using SqlTransaction transaction = connection.BeginTransaction();

      using SqlCommand item = connection.CreateCommand();
      item.CommandText = createItem;
      item.Transaction = transaction;

      item.Parameters.AddWithValue(nameof(catalogItemId), catalogItemId);

      int newItemId = (int)await item.ExecuteScalarAsync(cancellationToken);

      using SqlCommand amount = connection.CreateCommand();
      item.CommandText = createStatus;
      item.Transaction = transaction;

      amount.Parameters.AddWithValue(nameof(catalogItemId), catalogItemId);
      amount.Parameters.AddWithValue(nameof(startingAmount), startingAmount);

      await amount.ExecuteNonQueryAsync(cancellationToken);
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "Failed to insert new item status into inventory for itemId = {CatalogItemId} with starting amount = {Amount}",
        catalogItemId,
        startingAmount
      );

      throw e;
    }
  }

  public async Task DeleteItemInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken = default)
  {
    string deleteStatus = $@"
USE [{_databaseName}];

DELETE FROM [Inventory]
WHERE ItemId = (
  SELECT TOP 1 I.ItemId FROM Items I
  WHERE I.CatalogItemId = @{nameof(catalogItemId)};
)
";

    try
    {
      await using SqlConnection connection = new SqlConnection(_connectionString);
      if (connection.State is ConnectionState.Closed)
        await connection.OpenAsync(cancellationToken);

      using SqlCommand command = connection.CreateCommand();
      command.CommandText = deleteStatus;
      command.Parameters.AddWithValue(nameof(catalogItemId), catalogItemId);

      await command.ExecuteNonQueryAsync(cancellationToken);
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "Could not delete status with catalog id = {CatalogItemId}",
        catalogItemId
      );

      throw e;
    }
  }

  public async Task<IEnumerable<InventoryStatus>> GetAllItemStatusesAsync(CancellationToken cancellationToken = default)
  {
    string getStatus = $@"
USE [{_databaseName}];

SELECT I.ItemId, I.CatalogItemId, IN.ItemCount
FROM [Items] I
  LEFT JOIN [Inventory] IN (I.ItemId = IN.ItemId);
";

    try
    {
      await using SqlConnection connection = new SqlConnection(_connectionString);
      if (connection.State is ConnectionState.Closed)
        await connection.OpenAsync(cancellationToken);

      using SqlCommand command = connection.CreateCommand();
      command.CommandText = getStatus;

      SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

      List<InventoryStatus> results = new List<InventoryStatus>();
      while (await reader.ReadAsync())
      {
        InventoryStatus status = new InventoryStatus()
        {
          ItemId = reader.GetInt32(0),
          CatalogItemId = reader.GetInt32(1),
          ItemCount = reader.GetInt32(2)
        };

        results.Add(status);
      }

      return results;
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "Could not get inventory status due to exception"
      );

      throw e;
    }
  }

  public async Task<InventoryStatus?> GetItemInventoryStatusAsync(int catalogItemId, CancellationToken cancellationToken = default)
  {
    string getStatus = $@"
USE [{_databaseName}];

SELECT I.ItemId, I.CatalogItemId, IN.ItemCount
FROM [Items] I
  LEFT JOIN [Inventory] IN (I.ItemId = IN.ItemId)
WHERE I.CatalogItemId = @{nameof(catalogItemId)};
";

    try
    {
      await using SqlConnection connection = new SqlConnection(_connectionString);
      if (connection.State is ConnectionState.Closed)
        await connection.OpenAsync(cancellationToken);

      using SqlCommand command = connection.CreateCommand();
      command.CommandText = getStatus;

      command.Parameters.AddWithValue(nameof(catalogItemId), catalogItemId);

      SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

      InventoryStatus? status = null;

      if (await reader.ReadAsync(cancellationToken))
        status = new InventoryStatus()
        {
          ItemId = reader.GetInt32(0),
          CatalogItemId = reader.GetInt32(1),
          ItemCount = reader.GetInt32(2)
        };

      return status;
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "Could not get inventory status for catalog item id = {CatalogItemId}",
        catalogItemId
      );

      throw e;
    }
  }

  public async Task<IEnumerable<InventoryStatus>> GetItemStatusesAsync(IEnumerable<int> catalogItemIds, CancellationToken cancellationToken = default)
  {
    string getStatus = $@"
USE [{_databaseName}];

SELECT I.ItemId, I.CatalogItemId, IN.ItemCount
FROM [Items] I
  LEFT JOIN [Inventory] IN (I.ItemId = IN.ItemId)
WHERE I.CatalogItemId in (@{nameof(catalogItemIds)});
";

    try
    {
      await using SqlConnection connection = new SqlConnection(_connectionString);
      if (connection.State is ConnectionState.Closed)
        await connection.OpenAsync(cancellationToken);

      using SqlCommand command = connection.CreateCommand();
      command.CommandText = getStatus;

      command.Parameters.AddWithValue(nameof(catalogItemIds), catalogItemIds);
      SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

      List<InventoryStatus> results = new List<InventoryStatus>();
      while (await reader.ReadAsync(cancellationToken))
      {
        InventoryStatus status = new InventoryStatus()
        {
          ItemId = reader.GetInt32(0),
          CatalogItemId = reader.GetInt32(1),
          ItemCount = reader.GetInt32(2)
        };

        results.Add(status);
      }

      return results;
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "Could not find status for catalog ids = {CatalogItemIds}",
        catalogItemIds
      );

      throw e;
    }
  }

  public async Task UpdateItemInventoryStatusAsync(int catalogItemId, int amount, CancellationToken cancellationToken = default)
  {
    string updateAmount = $@"
USE [{_databaseName}];

UPDATE [Inventory]
SET ItemCount = @{nameof(amount)}
WHERE ItemId = (
  SELECT TOP 1 I.ItemId FROM Items I
  WHERE I.CatalogItemId = @{nameof(catalogItemId)}
);
";

    try
    {
      await using SqlConnection connection = new SqlConnection(_connectionString);
      if (connection.State is ConnectionState.Closed)
        await connection.OpenAsync(cancellationToken);

      using SqlCommand command = connection.CreateCommand();
      command.CommandText = updateAmount;

      command.Parameters.AddWithValue(nameof(catalogItemId), catalogItemId);
      command.Parameters.AddWithValue(nameof(amount), amount);

      await command.ExecuteNonQueryAsync(cancellationToken);
    }
    catch (Exception e)
    {
      _logger.LogError(
        e,
        "Could not update catalog item with id = {CatalogItemId} to the new amount = {Amount}",
        catalogItemId,
        amount
      );

      throw e;
    }
  }
}
