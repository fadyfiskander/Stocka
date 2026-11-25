using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Inventory.Application.Bindings.StockCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.Stock;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.Adapters.StockCases;

/// <summary>
/// Implements the IStockCases module boundary contract. Commands are dispatched through the
/// application's MediatR pipeline (validation behavior, unit-of-work behavior, handler); queries
/// hit the read-side repositories directly, scoped to the current Organization.
/// Registered in DI as the implementation of the module boundary contract.
/// </summary>
public class StockCases : IStockCases
{
    private readonly IMediator _mediator;
    private readonly IStockMovementQueryRepository _stockMovementQueryRepository;
    private readonly IInventoryReportQueryRepository _reportRepository;
    private readonly IWarehouseBatchQueryRepository _warehouseBatchQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public StockCases(
        IMediator mediator,
        IStockMovementQueryRepository stockMovementQueryRepository,
        IInventoryReportQueryRepository reportRepository,
        IWarehouseBatchQueryRepository warehouseBatchQueryRepository,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _stockMovementQueryRepository = stockMovementQueryRepository;
        _reportRepository = reportRepository;
        _warehouseBatchQueryRepository = warehouseBatchQueryRepository;
        _currentUser = currentUser;
    }

    private Guid OrganizationId =>
        _currentUser.OrganizationId
        ?? throw new InvalidOperationException("OrganizationId is missing from the current user context.");

    private static int Page(int page) => Math.Max(1, page);
    private static int Size(int pageSize) => Math.Clamp(pageSize, 1, 100);

    // ---- Commands (through the MediatR pipeline) ----

    public Task<AdjustmentResultDto> AdjustStockAsync(AdjustStockCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new AdjustStockCommandMediatR(command), cancellationToken);

    public Task<AllocateStockResultDto> AllocateStockAsync(AllocateStockCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new AllocateStockCommandMediatR(command), cancellationToken);

    public Task<TransferStockResultDto> TransferStockAsync(TransferStockCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new TransferStockCommandMediatR(command), cancellationToken);

    public Task<StockMovementDto> WriteOffDamagedStockAsync(WriteOffDamagedStockCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new WriteOffDamagedStockCommandMediatR(command), cancellationToken);

    public Task<StockMovementDto> ReverseStockMovementAsync(ReverseStockMovementCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new ReverseStockMovementCommandMediatR(command), cancellationToken);

    public Task<StockMovementDto> RecordSaleOutAsync(RecordSaleOutCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new RecordSaleOutCommandMediatR(command), cancellationToken);

    public Task<StockMovementDto> RecordCustomerReturnAsync(RecordCustomerReturnCommand command, CancellationToken cancellationToken = default)
        => _mediator.Send(new RecordCustomerReturnCommandMediatR(command), cancellationToken);

    // ---- Queries (direct repository access) ----

    public async Task<PaginatedResult<StockMovementDto>> ListMovementsAsync(ListMovementsQuery query, CancellationToken cancellationToken = default)
    {
        var criteria = new MovementQueryCriteria
        {
            WarehouseId = query.WarehouseId.HasValue && query.WarehouseId.Value != Guid.Empty
                ? WarehouseId.From(query.WarehouseId.Value) : null,
            BatchId = query.BatchId.HasValue && query.BatchId.Value != Guid.Empty
                ? BatchId.From(query.BatchId.Value) : null,
            ProductId = query.ProductId.HasValue && query.ProductId.Value != Guid.Empty
                ? query.ProductId.Value : null,
            MovementTypes = query.MovementTypes,
            DateFromUtc = query.DateFromUtc,
            DateToUtc = query.DateToUtc,
            Page = Page(query.Page),
            PageSize = Size(query.PageSize)
        };

        return await _stockMovementQueryRepository.GetPagedAsync(criteria, cancellationToken);
    }

    public async Task<ProductStockDistributionDto> GetProductStockAsync(GetProductStockQuery query, CancellationToken cancellationToken = default)
    {
        if (query.ProductId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(query.ProductId));

        var rows = await _reportRepository.GetProductStockDistributionAsync(query.ProductId, cancellationToken);
        if (query.WarehouseId is { } wid && wid != Guid.Empty)
            rows = rows.Where(r => r.WarehouseId == wid).ToList();

        var total = await _reportRepository.GetProductTotalUnitsAsync(query.ProductId, cancellationToken);

        return new ProductStockDistributionDto(query.ProductId, total, rows.ToList());
    }

    public async Task<int> GetProductUnallocatedUnitsAsync(GetProductUnallocatedUnitsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.ProductId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(query.ProductId));

        return await _reportRepository.GetProductUnallocatedUnitsAsync(query.ProductId, cancellationToken);
    }

    public async Task<int> GetBatchQuantityInWarehouseAsync(GetBatchQuantityInWarehouseQuery query, CancellationToken cancellationToken = default)
        => await _warehouseBatchQueryRepository.GetQuantityInWarehouseAsync(
            WarehouseId.From(query.WarehouseId),
            BatchId.From(query.BatchId),
            cancellationToken);

    public async Task<int> GetWarehouseTotalUnitsAsync(GetWarehouseTotalUnitsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.WarehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse ID cannot be empty.", nameof(query.WarehouseId));

        var placements = await _warehouseBatchQueryRepository.GetAllAsync(WarehouseId.From(query.WarehouseId), cancellationToken);
        return placements.Sum(p => p.Quantity);
    }
}
