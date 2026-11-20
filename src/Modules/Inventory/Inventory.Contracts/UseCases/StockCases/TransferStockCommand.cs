using Inventory.Contracts.DTOs.Stock;

namespace Inventory.Contracts.UseCases.StockCases;

/// <summary>
/// Moves units of a batch between two warehouses of the current Organization as ONE business operation:
/// TransferOut at the source + TransferIn at the destination, sharing one Transfer reference id,
/// all persisted in a single SaveChanges. A partial commit cannot occur â€” if the source deduction
/// fails, the whole operation rolls back.
/// </summary>
public record TransferStockCommand
{
    /// <summary>The batch whose units are being moved.</summary>
    public Guid BatchId { get; init; }

    /// <summary>The warehouse the units leave.</summary>
    public Guid SourceWarehouseId { get; init; }

    /// <summary>The warehouse the units enter (must be active).</summary>
    public Guid DestinationWarehouseId { get; init; }

    /// <summary>Number of units to move; the source must hold at least this quantity.</summary>
    public int Quantity { get; init; }

    /// <summary>Optional audit note stored on both transfer legs.</summary>
    public string? Reason { get; init; }
}
