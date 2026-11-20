using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.DTOs.Stock;
using Inventory.Contracts.DTOs.StockMovement;

namespace Inventory.Contracts.UseCases.StockCases;

/// <summary>
/// Stock use case contract: the pure module boundary for every stock movement operation
/// (adjustments, allocation, transfers, sale/creturns, movement history and availability reads).
/// Implemented by the Inventory Application layer (which wires each call into its MediatR
/// pipeline — validation and unit-of-work included) and consumed via DI.
/// </summary>
public interface IStockCases
{
    #region Commands
    /// <summary>Applies a physical-count correction to one warehouse/batch placement.</summary>
    Task<AdjustmentResultDto> AdjustStockAsync(AdjustStockCommand command, CancellationToken cancellationToken = default);

    /// <summary>Places a batch's unallocated units into a warehouse.</summary>
    Task<AllocateStockResultDto> AllocateStockAsync(AllocateStockCommand command, CancellationToken cancellationToken = default);

    /// <summary>Moves units of a batch between two warehouses as one transfer pair.</summary>
    Task<TransferStockResultDto> TransferStockAsync(TransferStockCommand command, CancellationToken cancellationToken = default);

    /// <summary>Writes damaged stock off a warehouse with a required reason.</summary>
    Task<StockMovementDto> WriteOffDamagedStockAsync(WriteOffDamagedStockCommand command, CancellationToken cancellationToken = default);

    /// <summary>Reverses a previously recorded movement by appending the opposite correction.</summary>
    Task<StockMovementDto> ReverseStockMovementAsync(ReverseStockMovementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Issues stock out of a warehouse against a sale/order.</summary>
    Task<StockMovementDto> RecordSaleOutAsync(RecordSaleOutCommand command, CancellationToken cancellationToken = default);

    /// <summary>Records a customer return into the same batch/warehouse it was sold from.</summary>
    Task<StockMovementDto> RecordCustomerReturnAsync(RecordCustomerReturnCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region Queries
    /// <summary>Retrieves a paged movement history with optional filters.</summary>
    Task<PaginatedResult<StockMovementDto>> ListMovementsAsync(ListMovementsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a product's organization-wide stock total and per-warehouse distribution.</summary>
    Task<ProductStockDistributionDto> GetProductStockAsync(GetProductStockQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the units of one product still unallocated across all its batches.</summary>
    Task<int> GetProductUnallocatedUnitsAsync(GetProductUnallocatedUnitsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the current quantity held by one batch in one warehouse.</summary>
    Task<int> GetBatchQuantityInWarehouseAsync(GetBatchQuantityInWarehouseQuery query, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the raw total units currently held in one warehouse (see query caveats).</summary>
    Task<int> GetWarehouseTotalUnitsAsync(GetWarehouseTotalUnitsQuery query, CancellationToken cancellationToken = default);
    #endregion
}
