using BuildingBlocks.Application.Common.Models;
using Inventory.Application.Bindings.ReportCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.ReportCases;
using MediatR;

namespace Inventory.Application.UseCases.ReportCases;

/// <summary>
/// Central report read-model handler: each Handle maps exactly one report query onto its
/// dedicated repository method, always scoped to the current Organization, with paging bounds clamped
/// in one place (the Page/Size helpers).
/// </summary>
public class ReportQueryHandlers :
    IRequestHandler<WarehouseStockSummaryQueryMediatR, IEnumerable<WarehouseStockSummaryDto>>,
    IRequestHandler<WarehousesRankedByStockQueryMediatR, PaginatedResult<WarehouseStockSummaryDto>>,
    IRequestHandler<WarehousesRankedByCostQueryMediatR, PaginatedResult<WarehouseStockSummaryDto>>,
    IRequestHandler<WarehouseInventoryQueryMediatR, PaginatedResult<WarehouseInventoryItemDto>>,
    IRequestHandler<ZeroStockWarehousesQueryMediatR, IEnumerable<WarehouseStockSummaryDto>>,
    IRequestHandler<InactiveWarehousesWithStockQueryMediatR, IEnumerable<WarehouseStockSummaryDto>>,
    IRequestHandler<ZeroStockProductsQueryMediatR, PaginatedResult<ZeroStockProductDto>>,
    IRequestHandler<BatchesWithZeroRemainingQueryMediatR, IEnumerable<BatchDto>>,
    IRequestHandler<MovementTypeTotalsQueryMediatR, IEnumerable<MovementTypeTotalsDto>>,
    IRequestHandler<InboundOutboundTotalsQueryMediatR, InboundOutboundSummaryDto>,
    IRequestHandler<TransferHistoryQueryMediatR, PaginatedResult<TransferHistoryDto>>,
    IRequestHandler<AdjustmentHistoryQueryMediatR, PaginatedResult<StockMovementDto>>,
    IRequestHandler<HistoricalBalanceQueryMediatR, PaginatedResult<HistoricalBalanceDto>>,
    IRequestHandler<InventorySummaryQueryMediatR, InventorySummaryDto>
{
    private readonly IInventoryReportQueryRepository _reportRepository;
    private readonly BuildingBlocks.Application.Abstractions.ICurrentUserService _currentUser;

    public ReportQueryHandlers(IInventoryReportQueryRepository reportRepository, BuildingBlocks.Application.Abstractions.ICurrentUserService currentUser)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
    }

    private Guid OrganizationId
    {
        get
        {
            var organizationId = _currentUser.OrganizationId;
            if (organizationId is null)
                throw new ArgumentNullException("organizationId is null, organizationId cannot be null");

            return organizationId.Value;
        }
    }

    private static int Page(int page) => Math.Max(1, page);
    private static int Size(int pageSize) => Math.Clamp(pageSize, 1, 100);

    public async Task<IEnumerable<WarehouseStockSummaryDto>> Handle(WarehouseStockSummaryQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetWarehouseStockSummaryAsync(OrganizationId);

    public async Task<PaginatedResult<WarehouseStockSummaryDto>> Handle(WarehousesRankedByStockQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetWarehousesRankedByStockAsync(OrganizationId, request.Contract.Descending, Page(request.Contract.Page), Size(request.Contract.PageSize));

    public async Task<PaginatedResult<WarehouseStockSummaryDto>> Handle(WarehousesRankedByCostQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetWarehousesRankedByCostAsync(OrganizationId, request.Contract.Descending, Page(request.Contract.Page), Size(request.Contract.PageSize));

    public async Task<PaginatedResult<WarehouseInventoryItemDto>> Handle(WarehouseInventoryQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetWarehouseInventoryAsync(OrganizationId, request.Contract.WarehouseId, Page(request.Contract.Page), Size(request.Contract.PageSize));

    public async Task<IEnumerable<WarehouseStockSummaryDto>> Handle(ZeroStockWarehousesQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetZeroStockWarehousesAsync(OrganizationId);

    public async Task<IEnumerable<WarehouseStockSummaryDto>> Handle(InactiveWarehousesWithStockQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetInactiveWarehousesWithStockAsync(OrganizationId);

    public async Task<PaginatedResult<ZeroStockProductDto>> Handle(ZeroStockProductsQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetZeroStockProductsAsync(OrganizationId, Page(request.Contract.Page), Size(request.Contract.PageSize));

    public async Task<IEnumerable<BatchDto>> Handle(BatchesWithZeroRemainingQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetBatchesWithZeroRemainingAsync(OrganizationId);

    public async Task<IEnumerable<MovementTypeTotalsDto>> Handle(MovementTypeTotalsQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetMovementTotalsByTypeAsync(OrganizationId, request.Contract.DateFromUtc, request.Contract.DateToUtc);

    public async Task<InboundOutboundSummaryDto> Handle(InboundOutboundTotalsQueryMediatR request, CancellationToken ct)
    {
        if (request.Contract.DateFromUtc > request.Contract.DateToUtc)
            throw new ArgumentException("Date range is invalid: start must not be after end.", nameof(request));
        return await _reportRepository.GetInboundOutboundTotalsAsync(OrganizationId, request.Contract.DateFromUtc, request.Contract.DateToUtc);
    }

    public async Task<PaginatedResult<TransferHistoryDto>> Handle(TransferHistoryQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetTransferHistoryAsync(OrganizationId, Page(request.Contract.Page), Size(request.Contract.PageSize), request.Contract.DateFromUtc, request.Contract.DateToUtc);

    public async Task<PaginatedResult<StockMovementDto>> Handle(AdjustmentHistoryQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetAdjustmentHistoryAsync(OrganizationId, Page(request.Contract.Page), Size(request.Contract.PageSize), request.Contract.DateFromUtc, request.Contract.DateToUtc);

    public async Task<PaginatedResult<HistoricalBalanceDto>> Handle(HistoricalBalanceQueryMediatR request, CancellationToken ct)
        => await _reportRepository.GetHistoricalBalanceAsync(OrganizationId, request.Contract.AsOfUtc, Page(request.Contract.Page), Size(request.Contract.PageSize));

    public async Task<InventorySummaryDto> Handle(InventorySummaryQueryMediatR request, CancellationToken ct)
    {
        var (totalWarehouses, activeWarehouses, totalBatches, exhaustedBatches, unitsInWarehouses, unallocatedUnits, totalCost) =
            await allScalars();
        // Aggregate the scalars in one shot for a single round-trip experience per metric.
        return new InventorySummaryDto(OrganizationId, totalWarehouses, activeWarehouses, totalBatches, exhaustedBatches, unitsInWarehouses, unallocatedUnits, totalCost, DateTime.UtcNow);
    }

    private async Task<(int, int, int, int, int, int, decimal)> allScalars()
    {
        var totalWarehouses = await _reportRepository.GetWarehouseCountAsync(OrganizationId);
        var activeWarehouses = await _reportRepository.GetActiveWarehouseCountAsync(OrganizationId);
        var totalBatches = await _reportRepository.GetBatchCountAsync(OrganizationId);
        var exhaustedBatches = await _reportRepository.GetExhaustedBatchCountAsync(OrganizationId);
        var unitsInWarehouses = await _reportRepository.GetTotalUnitsInWarehousesAsync(OrganizationId);
        var unallocatedUnits = await _reportRepository.GetTotalUnallocatedUnitsAsync(OrganizationId);
        var totalCost = await _reportRepository.GetTotalInventoryCostAsync(OrganizationId);
        return (totalWarehouses, activeWarehouses, totalBatches, exhaustedBatches, unitsInWarehouses, unallocatedUnits, totalCost);
    }
}
