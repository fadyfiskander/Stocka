using FluentValidation;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.StockMovements;
using Inventory.Primitives.Identifiers;

namespace Inventory.Application.Validators.StockCases;

#region Validator
/// <summary>
/// Validates the command so a sale issue always references concrete identifiers, a positive
/// quantity, and an optional reason within the domain's <see cref="StockMovement.ReasonMaxLength"/> limit.
/// </summary>
public class RecordSaleOutCommandValidator : AbstractValidator<RecordSaleOutCommand>
{
    public RecordSaleOutCommandValidator()
    {
        RuleFor(x => x.BatchId).NotEqual(Guid.Empty);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(StockMovement.ReasonMaxLength)
            .When(x => x.Reason != null);
    }
}
#endregion
