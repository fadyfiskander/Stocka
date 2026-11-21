using FluentValidation;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.StockMovements;
using Inventory.Primitives.Identifiers;

namespace Inventory.Application.Validators.StockCases;

#region Validator
/// <summary>
/// Validates the command so an adjustment always carries a non-negative counted quantity and a
/// non-empty reason within the domain's <see cref="StockMovement.ReasonMaxLength"/> limit.
/// </summary>
public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.BatchId).NotEqual(Guid.Empty);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty);
        RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(StockMovement.ReasonMaxLength);
    }
}
#endregion
