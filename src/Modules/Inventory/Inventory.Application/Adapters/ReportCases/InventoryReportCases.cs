using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.ReportCases;

namespace Inventory.Application.UseCases.ReportCases;

/// <summary>
/// Implements the IInventoryReportCases module boundary contract by mapping each report query
/// directly onto its repository method, scoped to the current Organization,
/// with paging bounds clamped in one place (the Page/Size helpers).
/// </summary>
public class InventoryReportCases : IInventoryReportCases
{
    private readonly IInventoryReportQueryRepository _reportRepository;
    private readonly ICurrentUserService _currentUser;

    public InventoryReportCases(IInventoryReportQueryRepository reportRepository, ICurrentUserService currentUser)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
    }

    private Guid OrganizationId =>
        _currentUser.OrganizationId
        ?? throw new InvalidOperationException("OrganizationId is missing from the current user context.");

    private static int Page(int page) => Math.Max(1, page);
    private static int Size(int pageSize) => Math.Clamp(pageSize, 1, 100);

    public async Task<IEnumerable<WarehouseStockSummaryDto>> GetWarehouseStockSummaryAsync(WarehouseStockSummaryQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetWarehouseStockSummaryAsync(cancellationToken);

    public async Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByStockAsync(WarehousesRankedByStockQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetWarehousesRankedByStockAsync(query.Descending, Page(query.Page), Size(query.PageSize));

    public async Task<PaginatedResult<WarehouseStockSummaryDto>> GetWarehousesRankedByCostAsync(WarehousesRankedByCostQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetWarehousesRankedByCostAsync(query.Descending, Page(query.Page), Size(query.PageSize));

    public async Task<PaginatedResult<WarehouseInventoryItemDto>> GetWarehouseInventoryAsync(WarehouseInventoryQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetWarehouseInventoryAsync(query.WarehouseId, Page(query.Page), Size(query.PageSize));

    public async Task<IEnumerable<WarehouseStockSummaryDto>> GetZeroStockWarehousesAsync(ZeroStockWarehousesQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetZeroStockWarehousesAsync(cancellationToken);

    public async Task<IEnumerable<WarehouseStockSummaryDto>> GetInactiveWarehousesWithStockAsync(InactiveWarehousesWithStockQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetInactiveWarehousesWithStockAsync(cancellationToken);

    public async Task<PaginatedResult<ZeroStockProductDto>> GetZeroStockProductsAsync(ZeroStockProductsQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetZeroStockProductsAsync(Page(query.Page), Size(query.PageSize));

    public async Task<IEnumerable<BatchDto>> GetBatchesWithZeroRemainingAsync(BatchesWithZeroRemainingQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetBatchesWithZeroRemainingAsync(cancellationToken);

    public async Task<IEnumerable<MovementTypeTotalsDto>> GetMovementTypeTotalsAsync(MovementTypeTotalsQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetMovementTotalsByTypeAsync(query.DateFromUtc, query.DateToUtc);

    public async Task<InboundOutboundSummaryDto> GetInboundOutboundTotalsAsync(InboundOutboundTotalsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.DateFromUtc > query.DateToUtc)
            throw new ArgumentException("Date range is invalid: start must not be after end.", nameof(query));

        return await _reportRepository.GetInboundOutboundTotalsAsync(query.DateFromUtc, query.DateToUtc);
    }

    public async Task<PaginatedResult<TransferHistoryDto>> GetTransferHistoryAsync(TransferHistoryQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetTransferHistoryAsync(Page(query.Page), Size(query.PageSize), query.DateFromUtc, query.DateToUtc);

    public async Task<PaginatedResult<StockMovementDto>> GetAdjustmentHistoryAsync(AdjustmentHistoryQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetAdjustmentHistoryAsync(Page(query.Page), Size(query.PageSize), query.DateFromUtc, query.DateToUtc);

    public async Task<PaginatedResult<HistoricalBalanceDto>> GetHistoricalBalanceAsync(HistoricalBalanceQuery query, CancellationToken cancellationToken = default)
        => await _reportRepository.GetHistoricalBalanceAsync(query.AsOfUtc, Page(query.Page), Size(query.PageSize));

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(InventorySummaryQuery query, CancellationToken cancellationToken = default)
    {
        var totalWarehouses = await _reportRepository.GetWarehouseCountAsync();
        var activeWarehouses = await _reportRepository.GetActiveWarehouseCountAsync();
        var totalBatches = await _reportRepository.GetBatchCountAsync();
        var exhaustedBatches = await _reportRepository.GetExhaustedBatchCountAsync();
        var unitsInWarehouses = await _reportRepository.GetTotalUnitsInWarehousesAsync();
        var unallocatedUnits = await _reportRepository.GetTotalUnallocatedUnitsAsync();
        var totalCost = await _reportRepository.GetTotalInventoryCostAsync();

        return new InventorySummaryDto(
            OrganizationId, totalWarehouses, activeWarehouses, totalBatches, exhaustedBatches,
            unitsInWarehouses, unallocatedUnits, totalCost, DateTime.UtcNow);
    }
}