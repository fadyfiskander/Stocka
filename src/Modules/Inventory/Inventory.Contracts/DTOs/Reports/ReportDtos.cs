using BuildingBlocks.Application.Common.Models;
using System;
using System.Collections.Generic;

namespace Inventory.Contracts.DTOs.Reports
{
    /// <summary>
    /// Per-warehouse stock rollup. TotalUnits is a RAW UNIT COUNT across products whose
    /// units of measure may differ â€” it is not a normalized measure of warehouse capacity.
    /// TotalCost is derived ONLY from the recorded per-batch unit costs; no inventory
    /// valuation policy (FIFO / weighted average / â€¦) has been approved yet.
    /// </summary>
    public record WarehouseStockSummaryDto(
        Guid WarehouseId,
        string Name,
        string Location,
        bool IsActive,
        int Type,
        int TotalUnits,
        int DistinctProducts,
        decimal TotalCost
    );

    /// <summary>One product/batch line inside a warehouse inventory breakdown.</summary>
    public record WarehouseInventoryItemDto(
        Guid WarehouseBatchId,
        Guid BatchId,
        Guid ProductId,
        int Quantity,
        decimal BatchUnitCost,
        decimal LineCost,
        DateTime BatchCreatedAt
    );

    public record ProductStockRowDto(
        Guid WarehouseId,
        int Quantity
    );

    /// <summary>Current stock of a product across the Organization.</summary>
    public record ProductStockDistributionDto(
        Guid ProductId,
        int TotalQuantity,
        IReadOnlyList<ProductStockRowDto> Distribution
    );

    /// <summary>Product with known batches but zero (or no) stock in any warehouse.</summary>
    public record ZeroStockProductDto(
        Guid ProductId,
        int InitialQuantity,
        int UnallocatedQuantity,
        int QuantityInWarehouses
    );

    public record MovementTypeTotalsDto(
        int MovementType,
        int TotalQuantity,
        decimal SignedQuantity,
        int MovementCount
    );

    /// <summary>
    /// Net inbound/outbound units within a date range. Transfer legs are EXCLUDED from both
    /// totals (a transfer moves units internally and would double-count); see the transfer
    /// history report for inbound/outbound transfer flows.
    /// </summary>
    public record InboundOutboundSummaryDto(
        DateTime DateFromUtc,
        DateTime DateToUtc,
        int InboundUnits,
        int OutboundUnits
    );

    /// <summary>
    /// One transfer business operation, reconstructed from its paired TransferOut/TransferIn
    /// movements sharing the same Transfer reference. Reported once (not double counted).
    /// </summary>
    public record TransferHistoryDto(
        Guid TransferReferenceId,
        DateTime OccurredAtUtc,
        Guid ProductId,
        Guid BatchId,
        int Quantity,
        Guid SourceWarehouseId,
        Guid DestinationWarehouseId
    );

    /// <summary>Stock balance reconstructed from movement history as of a date (inclusive).</summary>
    public record HistoricalBalanceDto(
        Guid ProductId,
        Guid WarehouseId,
        int Quantity,
        int MovementsConsidered
    );

    /// <summary>Organization-level scalar rollup used by the public Inventory report contract.</summary>
    public record InventorySummaryDto(
        Guid OrganizationId,
        int TotalWarehouses,
        int ActiveWarehouses,
        int TotalBatches,
        int ExhaustedBatches,
        int UnitsInWarehouses,
        int UnallocatedUnits,
        decimal TotalInventoryCost,
        DateTime GeneratedAtUtc
    );
}

