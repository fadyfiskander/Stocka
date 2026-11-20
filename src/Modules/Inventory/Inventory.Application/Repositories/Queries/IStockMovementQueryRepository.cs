using BuildingBlocks.Application.Common.Models;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Primitives.Identifiers;

namespace Inventory.Application.Repositories.Queries
{
    /// <summary>Filter criteria for paged movement history queries.</summary>
    public record MovementQueryCriteria
    {
        public WarehouseId? WarehouseId { get; init; }
        public BatchId? BatchId { get; init; }
        public Guid? ProductId { get; init; }
        public IReadOnlyList<int>? MovementTypes { get; init; }
        /// <summary>Inclusive lower date-time bound (UTC). Null = unbounded.</summary>
        public DateTime? DateFromUtc { get; init; }
        /// <summary>Inclusive upper date-time bound (UTC). Null = unbounded.</summary>
        public DateTime? DateToUtc { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public interface IStockMovementQueryRepository
    {
        Task<StockMovementDto?> GetByIdAsync(Guid organizationId, StockMovementId id);
        Task<IEnumerable<StockMovementDto>> GetAllAsync(Guid organizationId, WarehouseId warehouseId);

        /// <summary>Batch movement (audit) history, oldest first.</summary>
        Task<IEnumerable<StockMovementDto>> GetByBatchAsync(Guid organizationId, BatchId batchId);

        /// <summary>Paged, filtered movement history (warehouse, product, batch, types, date range).</summary>
        Task<PaginatedResult<StockMovementDto>> GetPagedAsync(Guid organizationId, MovementQueryCriteria criteria);
    }
}
