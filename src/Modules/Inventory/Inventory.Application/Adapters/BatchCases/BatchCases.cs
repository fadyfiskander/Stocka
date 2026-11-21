using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Inventory.Application.Bindings.BatchCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.BatchCases;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.Adapters.BatchCases;

/// <summary>
/// Implements the IBatchCases contract by dispatching each pure contract record through the
/// application's MediatR pipeline (validation behavior, unit-of-work behavior, handler).
/// Registered in DI as the implementation of the module boundary contract.
/// </summary>
public class BatchCases(
    IMediator mediator, 
    IBatchQueryRepository batchQuery,
    IWarehouseBatchQueryRepository warehouseBatchQuery,
    IStockMovementQueryRepository stockMovementQuery) : IBatchCases
{
    public async Task<BatchDetailsDto> RetrieveBatchAsync(RetrieveBatchQuery query, CancellationToken cancellationToken = default)
    {
        if (query.BatchId == Guid.Empty)
            throw new ArgumentException("Batch ID cannot be empty.", nameof(query.BatchId));

        var batch = await batchQuery.GetByIdAsync(BatchId.From(query.BatchId), cancellationToken)
            ?? throw new KeyNotFoundException($"Batch '{query.BatchId}' was not found.");

        var quantityInWarehouses = await warehouseBatchQuery.GetTotalQuantityInWarehousesAsync(
            BatchId.From(batch.Id), cancellationToken);

        return new BatchDetailsDto(
            batch.Id,
            batch.ProductId,
            batch.PurchaseItemId,
            batch.OrganizationId,
            batch.InitialQuantity,
            batch.RemainingQuantity,
            batch.InitialQuantity - batch.RemainingQuantity,
            quantityInWarehouses,
            batch.RemainingQuantity == 0,
            batch.UnitCost,
            batch.InitialQuantity * batch.UnitCost,
            batch.RemainingQuantity * batch.UnitCost,
            batch.CreatedAt);

    }

    public async Task<PaginatedResult<BatchDto>> ListBatchesAsync(ListBatchesQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);


        if (query.WarehouseId.HasValue && query.WarehouseId.Value != Guid.Empty)
        {
            var warehouseBatches = await batchQuery.GetByWarehouseAsync(WarehouseId.From(query.WarehouseId.Value), cancellationToken);
            return warehouseBatches.ToPaginated(page, pageSize);
        }

        if (query.ProductId.HasValue && query.ProductId.Value != Guid.Empty)
            return await batchQuery.GetByProductAsync(query.ProductId.Value, page, pageSize, cancellationToken);

        if (query.PurchaseItemId.HasValue && query.PurchaseItemId.Value != Guid.Empty)
            return await batchQuery.GetByPurchaseItemAsync(query.PurchaseItemId.Value, page, pageSize, cancellationToken);

        return await batchQuery.GetAllPagedAsync(page, pageSize, cancellationToken);
    }

    public async Task<PaginatedResult<BatchDto>> ListUnallocatedBatchesAsync(ListUnallocatedBatchesQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var batches = await batchQuery.GetUnallocatedAsync(cancellationToken);
        return batches.ToPaginated(page, pageSize);
    }

    public async Task<IEnumerable<StockMovementDto>> RetrieveBatchMovementHistoryAsync(RetrieveBatchMovementHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.BatchId == Guid.Empty)
            throw new ArgumentException("Batch ID cannot be empty.", nameof(query.BatchId));

        // Existence check scoped to the current Organization (cross-Organization => not found).
        return await stockMovementQuery.GetByBatchAsync(BatchId.From(query.BatchId), cancellationToken);
    }

    public Task<BatchDto> RegisterBatchAsync(RegisterBatchCommand command, CancellationToken cancellationToken = default)
        => mediator.Send(new RegisterBatchCommandMediatR(command), cancellationToken);

}