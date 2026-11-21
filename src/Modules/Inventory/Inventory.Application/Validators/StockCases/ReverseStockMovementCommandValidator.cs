using FluentValidation;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.StockMovements;

namespace Inventory.Application.Validators.StockCases;

#region Validator
/// <summary>
/// Validates the command so a reversal always references a concrete movement and a non-empty
/// reason within the domain's <see cref="StockMovement.ReasonMaxLength"/> limit.
/// </summary>
public class ReverseStockMovementCommandValidator : AbstractValidator<ReverseStockMovementCommand>
{
    public ReverseStockMovementCommandValidator()
    {
        RuleFor(x => x.MovementId).NotEqual(Guid.Empty);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(StockMovement.ReasonMaxLength);
    }
}
#endregion
