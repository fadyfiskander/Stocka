using FluentValidation;
using Inventory.Contracts.UseCases.WarehouseCases;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Validators.WarehouseCases;

#region Validator
/// <summary>
/// Validates the provided fields only: length rules mirror the physical database constraints
/// (NVARCHAR(50)/(100)/(600)), and a provided Type must be a defined <see cref="WarehouseType"/> value.
/// </summary>
public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).MaximumLength(50).When(x => x.Name != null);
        RuleFor(x => x.Location).MaximumLength(100).When(x => x.Location != null);
        RuleFor(x => x.Description).MaximumLength(600).When(x => x.Description != null);
        RuleFor(x => x.Type).Must(t => !t.HasValue || Enum.IsDefined(typeof(WarehouseType), t.Value))
            .WithMessage("Invalid warehouse type.");
    }
}
#endregion
