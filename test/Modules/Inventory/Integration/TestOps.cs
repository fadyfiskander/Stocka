using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.DTOs.Warehouse;
using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Contracts.UseCases.WarehouseCases;
using Inventory.Domain.Warehouses;

namespace Inventory.Integration.Tests;

/// <summary>Shared command-driven setup for integration scenario tests, dispatched through the pure contract use-case interfaces.</summary>
internal static class TestOps
{
    public static async Task<(BatchDto Batch, WarehouseDto Warehouse)> CreateStockAsync(
        IWarehouseCases warehouseCases, IBatchCases batchCases, IStockCases stockCases,
        string tag, int initialQuantity, int allocatedQuantity)
    {
        var warehouse = await warehouseCases.RegisterWarehouseAsync(new RegisterWarehouseCommand
        {
            Name = "WH " + tag,
            Location = "Location " + tag,
            Type = (int)WarehouseType.Shop
        });

        var batch = await batchCases.RegisterBatchAsync(new RegisterBatchCommand
        {
            ProductId = Guid.NewGuid(),
            PurchaseItemId = Guid.NewGuid(),
            InitialQuantity = initialQuantity,
            UnitCost = 10m
        });

        if (allocatedQuantity > 0)
        {
            await stockCases.AllocateStockAsync(new AllocateStockCommand
            {
                BatchId = batch.Id,
                WarehouseId = warehouse.Id,
                Quantity = allocatedQuantity
            });
        }

        return (batch, warehouse);
    }

    public static Task<int> WarehouseQuantityAsync(IStockCases stockCases, BatchDto batch, WarehouseDto warehouse) =>
        stockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batch.Id, warehouse.Id));

    public static async Task<List<StockMovementDto>> BatchHistoryAsync(IBatchCases batchCases, BatchDto batch) =>
        (await batchCases.RetrieveBatchMovementHistoryAsync(new RetrieveBatchMovementHistoryQuery(batch.Id))).ToList();
}
