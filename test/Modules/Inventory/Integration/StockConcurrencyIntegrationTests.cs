using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Integration.Tests;

/// <summary>
/// Optimistic-concurrency coverage: RowVersion conflicts must never allow a silent
/// lost update or a silent overdraw; they surface as an application-level retry message.
/// </summary>
[Collection("InventoryDatabase")]
public class StockConcurrencyIntegrationTests
{
    private readonly InventoryDatabaseFixture _fixture;

    public StockConcurrencyIntegrationTests(InventoryDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Stale_rowversion_surfaces_a_clear_retry_message_and_blocks_the_write()
    {
        var organizationId = Guid.NewGuid();

        Guid warehouse, batchId, destination;
        using (var setup = _fixture.NewScope(organizationId))
        {
            var (b, w) = await TestOps.CreateStockAsync(setup.WarehouseCases, setup.BatchCases, setup.StockCases, $"ccy-{Guid.NewGuid():N}", 20, 10);
            warehouse = w.Id;
            batchId = b.Id;

            var destinationWarehouse = await setup.WarehouseCases.RegisterWarehouseAsync(new Inventory.Contracts.UseCases.WarehouseCases.RegisterWarehouseCommand
            {
                Name = "CCY-DEST " + Guid.NewGuid().ToString("N"),
                Location = "Elsewhere",
                Type = (int)WarehouseType.Storage
            });
            destination = destinationWarehouse.Id;
        }

        using var writer = _fixture.NewScope(organizationId);
        var sender = writer.Sender;
        var db = writer.Db;

        // Prematerialize the warehouse batch in THIS scope's context (identity-resolved later).
        var tracked = await db.Set<WarehouseBatch>().SingleAsync(wb =>
            wb.WarehouseId == WarehouseId.From(warehouse)
            && wb.BatchId == BatchId.From(batchId));

        // Another writer commits a change first: quantity 10 -> 9 and the rowversion is bumped.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE [Inventory].[WarehouseBatches] SET [Quantity] = [Quantity] - 1 WHERE [Id] = {0}",
            tracked.Id.Value);

        // The stale scope now issues a transfer that would deduct 5 from 9 (allowed) - but its copy
        // was loaded before the other writer's commit: the rowversion check must reject the write.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => writer.StockCases.TransferStockAsync(new TransferStockCommand
        {
            BatchId = batchId,
            SourceWarehouseId = warehouse,
            DestinationWarehouseId = destination,
            Quantity = 5
        }));

        Assert.Contains("modified by another operation", ex.Message, StringComparison.OrdinalIgnoreCase);

        // The concurrent writer's committed value stands; nothing was lost.
        var quantityNow = await writer.StockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batchId, warehouse));
        Assert.Equal(9, quantityNow);
        Assert.Equal(0, await writer.StockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batchId, destination)));
    }
}
