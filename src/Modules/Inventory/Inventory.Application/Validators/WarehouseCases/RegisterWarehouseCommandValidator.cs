using FluentValidation;
using Inventory.Contracts.UseCases.WarehouseCases;
using Inventory.Domain.Enums;

namespace Inventory.Application.Validators.WarehouseCases;

#region Validator
/// <summary>
/// Pipeline-level validation matching the physical database constraints
/// (NVARCHAR(50)/(100)/(600)) so validation failures surface as 400s instead of SQL errors.
/// </summary>
public class RegisterWarehouseCommandValidator : AbstractValidator<RegisterWarehouseCommand>
{
    public RegisterWarehouseCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(600);

        RuleFor(x => x.Type)
            .Must(t => Enum.IsDefined(typeof(WarehouseType), t))
            .WithMessage("Invalid warehouse type.");
    }
}
#endregion
