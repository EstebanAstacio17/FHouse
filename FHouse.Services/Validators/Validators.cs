using FluentValidation;
using FHouse.Core.Enums;
using FHouse.Services.DTOs;

namespace FHouse.Services.Validators
{
    public class CrearTransaccionValidator : AbstractValidator<CrearTransaccionDto>
    {
        public CrearTransaccionValidator()
        {
            RuleFor(x => x.Concepto)
                .NotEmpty().WithMessage("El concepto de la transacción es obligatorio.")
                .MaximumLength(250).WithMessage("El concepto no puede exceder los 250 caracteres.");

            RuleFor(x => x.Monto)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
                .LessThanOrEqualTo(999999999m).WithMessage("El monto excede el límite permitido.");

            RuleFor(x => x.FamiliaId)
                .GreaterThan(0).WithMessage("La familia es requerida.");

            RuleFor(x => x.CuentaOrigenId)
                .NotNull().When(x => x.Tipo == TipoTransaccion.Egreso)
                .WithMessage("Debe seleccionar una cuenta de origen para un egreso.");
        }
    }

    public class CrearFuenteIngresoValidator : AbstractValidator<CrearFuenteIngresoDto>
    {
        public CrearFuenteIngresoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre de la fuente de ingreso es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

            RuleFor(x => x.FamiliaId)
                .GreaterThan(0).WithMessage("La familia es requerida.");
        }
    }

    public class CrearCuentaValidator : AbstractValidator<CrearCuentaDto>
    {
        public CrearCuentaValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre de la cuenta es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

            RuleFor(x => x.FamiliaId)
                .GreaterThan(0).WithMessage("La familia es requerida.");
        }
    }

    public class TransferenciaCuentaValidator : AbstractValidator<TransferenciaCuentaDto>
    {
        public TransferenciaCuentaValidator()
        {
            RuleFor(x => x.Monto)
                .GreaterThan(0).WithMessage("El monto de la transferencia debe ser mayor a 0.");

            RuleFor(x => x.CuentaOrigenId)
                .GreaterThan(0).WithMessage("Debe seleccionar una cuenta de origen.");

            RuleFor(x => x.CuentaDestinoId)
                .GreaterThan(0).WithMessage("Debe seleccionar una cuenta de destino.")
                .NotEqual(x => x.CuentaOrigenId).WithMessage("La cuenta de destino no puede ser igual a la de origen.");
        }
    }

    public class CrearPresupuestoValidator : AbstractValidator<CrearPresupuestoDto>
    {
        public CrearPresupuestoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del presupuesto es obligatorio.");

            RuleFor(x => x.MontoLimite)
                .GreaterThan(0).WithMessage("El monto límite debe ser mayor a 0.");

            RuleFor(x => x.Mes)
                .InclusiveBetween(1, 12).WithMessage("El mes debe estar entre 1 y 12.");

            RuleFor(x => x.Anio)
                .GreaterThanOrEqualTo(2020).WithMessage("El año no es válido.");
        }
    }
}
