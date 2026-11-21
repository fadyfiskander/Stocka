using FluentValidation;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.Entities;

namespace Inventory.Application.Validators.StockCases;

#region Validator
/// <summary>
/// Validates the command so a transfer always references concrete identifiers, a positive
/// quantity, and an optional reason within the domain's <see cref="StockMovement.ReasonMaxLength"/> limit.
/// </summary>
public class TransferStockCommandValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockCommandValidator()
    {
        RuleFor(x => x.BatchId).NotEqual(Guid.Empty);
        RuleFor(x => x.SourceWarehouseId).NotEqual(Guid.Empty);
        RuleFor(x => x.DestinationWarehouseId).NotEqual(Guid.Empty);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(StockMovement.ReasonMaxLength).When(x => x.Reason != null);
    }
}
#endregion
