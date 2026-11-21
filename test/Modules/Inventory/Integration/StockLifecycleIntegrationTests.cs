using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Contracts.UseCases.WarehouseCases;
using Inventory.Domain.StockMovements;
using Inventory.Domain.Warehouses;

namespace Inventory.Integration.Tests;

/// <summary>
/// End-to-end stock lifecycle against a real SQL Server (LocalDB) database:
/// allocation, transfer, sale, return, adjustment, damage write-off, reversal,
/// organization isolation and atomicity.
/// </summary>
[Collection("InventoryDatabase")]
public class StockLifecycleIntegrationTests
{
    private readonly InventoryDatabaseFixture _fixture;

    public StockLifecycleIntegrationTests(InventoryDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Allocate_decrements_unallocated_creates_warehouse_stock_and_records_PurchaseIn()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"alloc-{Guid.NewGuid():N}", 100, 0);
        Assert.Equal(100, await scope.StockCases.GetProductUnallocatedUnitsAsync(new GetProductUnallocatedUnitsQuery(batch.ProductId)));

        var result = await scope.StockCases.AllocateStockAsync(new AllocateStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 30
        });

        Assert.Equal(70, result.BatchRemainingQuantity);
        Assert.Equal(30, result.QuantityInWarehouse);
        Assert.Equal(30, await scope.StockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batch.Id, warehouse.Id)));
        Assert.Equal(70, await scope.StockCases.GetProductUnallocatedUnitsAsync(new GetProductUnallocatedUnitsQuery(batch.ProductId)));

        var history = await TestOps.BatchHistoryAsync(scope.BatchCases, batch);
        var purchaseIn = history.Single(m => m.MovementType == (int)StockMovementType.PurchaseIn);
        Assert.Equal((int)StockReferenceType.Purchase, purchaseIn.ReferenceType);
        Assert.Equal(batch.PurchaseItemId, purchaseIn.ReferenceId);
    }

    [Fact]
    public async Task Allocate_more_than_unallocated_fails_and_leaves_batch_unchanged()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"over-alloc-{Guid.NewGuid():N}", 10, 0);

        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.StockCases.AllocateStockAsync(new AllocateStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 11
        }));

        Assert.Equal(10, await scope.StockCases.GetProductUnallocatedUnitsAsync(new GetProductUnallocatedUnitsQuery(batch.ProductId)));
        Assert.Equal(0, await scope.StockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batch.Id, warehouse.Id)));
    }

    [Fact]
    public async Task Allocate_to_an_existing_warehouse_batch_increases_its_quantity()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"alloc-existing-{Guid.NewGuid():N}", 100, 30);

        var result = await scope.StockCases.AllocateStockAsync(new AllocateStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 10
        });

        Assert.Equal(40, result.QuantityInWarehouse);
        Assert.Equal(60, result.BatchRemainingQuantity);
        Assert.Equal(40, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, warehouse));
    }

    [Fact]
    public async Task Transfer_moves_stock_and_records_one_shared_reference_pair()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, sourceWarehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"src-{Guid.NewGuid():N}", 50, 20);

        var destination = await scope.WarehouseCases.RegisterWarehouseAsync(new RegisterWarehouseCommand
        {
            Name = "DEST " + Guid.NewGuid().ToString("N"),
            Location = "Elsewhere",
            Type = (int)WarehouseType.Storage
        });

        var transfer = await scope.StockCases.TransferStockAsync(new TransferStockCommand
        {
            BatchId = batch.Id,
            SourceWarehouseId = sourceWarehouse.Id,
            DestinationWarehouseId = destination.Id,
            Quantity = 5
        });

        Assert.Equal(15, transfer.QuantityInSource);
        Assert.Equal(5, transfer.QuantityInDestination);
        Assert.NotEqual(Guid.Empty, transfer.TransferReferenceId);

        Assert.Equal(5, await scope.StockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batch.Id, destination.Id)));
        Assert.Equal(15, await scope.StockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batch.Id, sourceWarehouse.Id)));

        var history = await TestOps.BatchHistoryAsync(scope.BatchCases, batch);
        var outLeg = history.Single(m => m.MovementType == (int)StockMovementType.TransferOut);
        var inLeg = history.Single(m => m.MovementType == (int)StockMovementType.TransferIn);

        Assert.Equal((int)StockReferenceType.Transfer, outLeg.ReferenceType);
        Assert.Equal((int)StockReferenceType.Transfer, inLeg.ReferenceType);
        Assert.NotEqual(Guid.Empty, outLeg.ReferenceId);
        Assert.Equal(outLeg.ReferenceId, inLeg.ReferenceId);
    }

    [Fact]
    public async Task Transfer_with_insufficient_source_fails_atomically()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, source) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"ins-{Guid.NewGuid():N}", 30, 10);
        var destination = await scope.WarehouseCases.RegisterWarehouseAsync(new RegisterWarehouseCommand
        {
            Name = "DEST " + Guid.NewGuid().ToString("N"),
            Location = "Elsewhere",
            Type = (int)WarehouseType.Storage
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.StockCases.TransferStockAsync(new TransferStockCommand
        {
            BatchId = batch.Id,
            SourceWarehouseId = source.Id,
            DestinationWarehouseId = destination.Id,
            Quantity = 11
        }));

        // Nothing partially committed: source unchanged, destination untouched, no new movements.
        Assert.Equal(10, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, source));
        Assert.Equal(0, await scope.StockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batch.Id, destination.Id)));
        var history = await TestOps.BatchHistoryAsync(scope.BatchCases, batch);
        Assert.Single(history); // only the setup allocation's PurchaseIn - the failed transfer recorded nothing
    }

    [Fact]
    public async Task Transfer_to_a_warehouse_that_already_has_the_batch_increases_its_quantity()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, source) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"txe-{Guid.NewGuid():N}", 50, 20);
        var destination = await scope.WarehouseCases.RegisterWarehouseAsync(new RegisterWarehouseCommand
        {
            Name = "DEST " + Guid.NewGuid().ToString("N"),
            Location = "Elsewhere",
            Type = (int)WarehouseType.Storage
        });

        await scope.StockCases.TransferStockAsync(new TransferStockCommand
        {
            BatchId = batch.Id,
            SourceWarehouseId = source.Id,
            DestinationWarehouseId = destination.Id,
            Quantity = 5
        });

        var secondTransfer = await scope.StockCases.TransferStockAsync(new TransferStockCommand
        {
            BatchId = batch.Id,
            SourceWarehouseId = source.Id,
            DestinationWarehouseId = destination.Id,
            Quantity = 4
        });

        Assert.Equal(11, secondTransfer.QuantityInSource);
        Assert.Equal(9, secondTransfer.QuantityInDestination);
        Assert.Equal(9, await scope.StockCases.GetBatchQuantityInWarehouseAsync(new GetBatchQuantityInWarehouseQuery(batch.Id, destination.Id)));
    }

    [Fact]
    public async Task Sale_reduces_warehouse_stock_without_touching_batch_unallocated()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"sale-{Guid.NewGuid():N}", 40, 10);

        var sale = await scope.StockCases.RecordSaleOutAsync(new RecordSaleOutCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 4,
            OrderReferenceId = Guid.NewGuid()
        });

        Assert.Equal((int)StockMovementType.SaleOut, sale.MovementType);
        Assert.Equal((int)StockReferenceType.Order, sale.ReferenceType);

        Assert.Equal(6, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, warehouse));
        Assert.Equal(30, await scope.StockCases.GetProductUnallocatedUnitsAsync(new GetProductUnallocatedUnitsQuery(batch.ProductId)));
    }

    [Fact]
    public async Task Customer_return_reenters_same_warehouse_and_does_not_change_unallocated()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"ret-{Guid.NewGuid():N}", 40, 10);
        await scope.StockCases.RecordSaleOutAsync(new RecordSaleOutCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 6
        });

        var beforeUnallocated = await scope.StockCases.GetProductUnallocatedUnitsAsync(new GetProductUnallocatedUnitsQuery(batch.ProductId));

        var ret = await scope.StockCases.RecordCustomerReturnAsync(new RecordCustomerReturnCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 2,
            OrderReferenceId = Guid.NewGuid()
        });

        Assert.Equal((int)StockMovementType.OrderReturn, ret.MovementType);
        Assert.Equal((int)StockReferenceType.Return, ret.ReferenceType);

        Assert.Equal(6, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, warehouse));
        Assert.Equal(beforeUnallocated, await scope.StockCases.GetProductUnallocatedUnitsAsync(new GetProductUnallocatedUnitsQuery(batch.ProductId)));
    }

    [Fact]
    public async Task Adjustment_is_idempotent_when_counted_equals_current()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"adj-{Guid.NewGuid():N}", 20, 10);

        var noChange = await scope.StockCases.AdjustStockAsync(new AdjustStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            CountedQuantity = 10,
            Reason = "Physical count - no change expected"
        });

        Assert.Null(noChange.MovementId);
        Assert.Equal(0, noChange.QuantityChange);

        var increase = await scope.StockCases.AdjustStockAsync(new AdjustStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            CountedQuantity = 12,
            Reason = "Count found two extra units"
        });
        Assert.Equal(2, increase.QuantityChange);
        Assert.NotNull(increase.MovementId);

        var decrease = await scope.StockCases.AdjustStockAsync(new AdjustStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            CountedQuantity = 9,
            Reason = "Two units broken"
        });
        Assert.Equal(-3, decrease.QuantityChange);

        Assert.Equal(9, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, warehouse));

        var history = await TestOps.BatchHistoryAsync(scope.BatchCases, batch);
        Assert.Equal(1, history.Count(m => m.MovementType == (int)StockMovementType.AdjustmentIn));
        Assert.Equal(1, history.Count(m => m.MovementType == (int)StockMovementType.AdjustmentOut));
        // Idempotency: still exactly one of each - recounting 10 again created nothing.
        await scope.StockCases.AdjustStockAsync(new AdjustStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            CountedQuantity = 9,
            Reason = "Recount same value"
        });
        history = await TestOps.BatchHistoryAsync(scope.BatchCases, batch);
        Assert.Equal(1, history.Count(m => m.MovementType == (int)StockMovementType.AdjustmentOut));
    }

    [Fact]
    public async Task Write_off_requires_reason_and_reduces_stock()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"dmg-{Guid.NewGuid():N}", 20, 10);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
        {
            return scope.StockCases.WriteOffDamagedStockAsync(new WriteOffDamagedStockCommand
            {
                BatchId = batch.Id,
                WarehouseId = warehouse.Id,
                Quantity = 2
            });
        });

        var damage = await scope.StockCases.WriteOffDamagedStockAsync(new WriteOffDamagedStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 2,
            Reason = "Water damage in aisle 3"
        });

        Assert.Equal((int)StockMovementType.AdjustmentOut, damage.MovementType);
        Assert.Equal(2, damage.Quantity);
        Assert.NotEmpty(damage.Reason!);

        Assert.Equal(8, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, warehouse));
    }

    [Fact]
    public async Task Reversal_of_sale_and_of_allocation_balance_stock_and_restore_unallocated()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"rev-{Guid.NewGuid():N}", 50, 10);
        var sale = await scope.StockCases.RecordSaleOutAsync(new RecordSaleOutCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 4
        });

        // Reverse the sale: units come back into current stock as AdjustmentIn.
        var saleReversal = await scope.StockCases.ReverseStockMovementAsync(new ReverseStockMovementCommand
        {
            MovementId = sale.Id,
            Reason = "Sale entered with wrong quantity"
        });

        Assert.Equal((int)StockMovementType.AdjustmentIn, saleReversal.MovementType);
        Assert.Equal((int)StockReferenceType.ReversalOfMovement, saleReversal.ReferenceType);
        Assert.Equal(sale.Id, saleReversal.ReferenceId);

        Assert.Equal(10, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, warehouse));
        var unallocatedAfterSaleReversal =
            await scope.StockCases.GetProductUnallocatedUnitsAsync(new GetProductUnallocatedUnitsQuery(batch.ProductId));
        Assert.Equal(40, unallocatedAfterSaleReversal);

        // Reverse the allocation (PurchaseIn): units return to unallocated stock.
        var history = await TestOps.BatchHistoryAsync(scope.BatchCases, batch);
        var purchaseIn = history.Single(m => m.MovementType == (int)StockMovementType.PurchaseIn);

        await scope.StockCases.ReverseStockMovementAsync(new ReverseStockMovementCommand
        {
            MovementId = purchaseIn.Id,
            Reason = "Allocation registered against wrong warehouse"
        });

        Assert.Equal(0, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, warehouse));
        Assert.Equal(50, await scope.StockCases.GetProductUnallocatedUnitsAsync(new GetProductUnallocatedUnitsQuery(batch.ProductId)));
    }

    [Fact]
    public async Task Transfer_leg_cannot_be_reversed()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, source) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"towrev-{Guid.NewGuid():N}", 30, 10);
        var destination = await scope.WarehouseCases.RegisterWarehouseAsync(new RegisterWarehouseCommand
        {
            Name = "DEST " + Guid.NewGuid().ToString("N"),
            Location = "Elsewhere",
            Type = (int)WarehouseType.Storage
        });

        await scope.StockCases.TransferStockAsync(new TransferStockCommand
        {
            BatchId = batch.Id,
            SourceWarehouseId = source.Id,
            DestinationWarehouseId = destination.Id,
            Quantity = 3
        });

        var history = await TestOps.BatchHistoryAsync(scope.BatchCases, batch);
        var transferOut = history.Single(m => m.MovementType == (int)StockMovementType.TransferOut);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.StockCases.ReverseStockMovementAsync(new ReverseStockMovementCommand
            {
                MovementId = transferOut.Id,
                Reason = "Trying to undo a transfer leg"
            }));

        Assert.Contains("Transfer", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reversal_requires_a_reason()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"no-rev-{Guid.NewGuid():N}", 20, 5);
        var sale = await scope.StockCases.RecordSaleOutAsync(new RecordSaleOutCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 2
        });

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            scope.StockCases.ReverseStockMovementAsync(new ReverseStockMovementCommand
            {
                MovementId = sale.Id,
                Reason = ""
            }));
    }

    [Fact]
    public async Task Deactivated_warehouse_rejects_new_stock_but_keeps_what_it_holds()
    {
        var organizationId = Guid.NewGuid();
        using var scope = _fixture.NewScope(organizationId);

        var (batch, warehouse) = await TestOps.CreateStockAsync(scope.WarehouseCases, scope.BatchCases, scope.StockCases, $"deact-{Guid.NewGuid():N}", 20, 5);

        var deactivated = await scope.WarehouseCases.DeactivateWarehouseAsync(new DeactivateWarehouseCommand(warehouse.Id));
        Assert.False(deactivated.IsActive);

        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.StockCases.AllocateStockAsync(new AllocateStockCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 3
        }));

        // Non-destructive: the current stock is still there and sellable.
        Assert.Equal(5, await TestOps.WarehouseQuantityAsync(scope.StockCases, batch, warehouse));
        var sale = await scope.StockCases.RecordSaleOutAsync(new RecordSaleOutCommand
        {
            BatchId = batch.Id,
            WarehouseId = warehouse.Id,
            Quantity = 1
        });
        Assert.Equal((int)StockMovementType.SaleOut, sale.MovementType);
    }

    [Fact]
    public async Task Cross_organization_ids_are_invisible_to_other_organizations()
    {
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        Guid warehouseA1, warehouseA2;
        using (var scopeA = _fixture.NewScope(organizationA))
        {
            var (batchA, warehouse1) = await TestOps.CreateStockAsync(scopeA.WarehouseCases, scopeA.BatchCases, scopeA.StockCases, $"iso-{Guid.NewGuid():N}", 10, 10);
            warehouseA1 = warehouse1.Id;

            var warehouse2 = await scopeA.WarehouseCases.RegisterWarehouseAsync(new RegisterWarehouseCommand
            {
                Name = "ISO-2 " + Guid.NewGuid().ToString("N"),
                Location = "Second site",
                Type = 0
            });
            warehouseA2 = warehouse2.Id;

            // Organization B cannot see or touch Organization A's warehouse/batch.
            using var scopeB = _fixture.NewScope(organizationB);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                scopeB.WarehouseCases.RetrieveWarehouseAsync(new RetrieveWarehouseQuery(warehouseA1)));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                scopeB.StockCases.TransferStockAsync(new TransferStockCommand
                {
                    BatchId = batchA.Id,
                    SourceWarehouseId = warehouseA1,
                    DestinationWarehouseId = warehouseA2,
                    Quantity = 1
                }));
        }

        // Organization A still sees exactly its own data (both warehouses, both reachable).
        using var readerA = _fixture.NewScope(organizationA);
        var activeWarehouses = (await readerA.WarehouseCases.ListWarehousesAsync(new ListWarehousesQuery { IsActive = true })).Items
            .Select(w => w.Id).ToList();
        Assert.Contains(warehouseA1, activeWarehouses);
        Assert.Contains(warehouseA2, activeWarehouses);
    }
}
