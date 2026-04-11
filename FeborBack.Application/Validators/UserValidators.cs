using FluentValidation;
using FeborBack.Application.DTOs.User;

namespace FeborBack.Application.Validators.User;

public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("El nombre de usuario es requerido")
            .MinimumLength(3)
            .WithMessage("El nombre de usuario debe tener al menos 3 caracteres")
            .MaximumLength(100)
            .WithMessage("El nombre de usuario no puede exceder 100 caracteres")
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("El nombre de usuario solo puede contener letras, números, puntos, guiones y guiones bajos");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(255)
            .WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("El nombre completo es requerido")
            .MinimumLength(2)
            .WithMessage("El nombre completo debe tener al menos 2 caracteres")
            .MaximumLength(255)
            .WithMessage("El nombre completo no puede exceder 255 caracteres");

        RuleFor(x => x.StatusId)
            .GreaterThan(0)
            .WithMessage("El estado es requerido");

        RuleFor(x => x.AvatarName)
            .MaximumLength(255)
            .WithMessage("El nombre del avatar no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.AvatarName));

        RuleForEach(x => x.RoleIds)
            .GreaterThan(0)
            .WithMessage("Los IDs de rol deben ser mayores a 0");
    }
}

public class UserFilterValidator : AbstractValidator<UserFilterDto>
{
    public UserFilterValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("El número de página debe ser mayor a 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("El tamaño de página debe estar entre 1 y 100");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .WithMessage("Campo de ordenamiento no válido");

        RuleFor(x => x.Username)
            .MaximumLength(100)
            .WithMessage("El nombre de usuario no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Username));

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.FullName)
            .MaximumLength(255)
            .WithMessage("El nombre completo no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.CreatedFrom)
            .LessThanOrEqualTo(x => x.CreatedTo)
            .WithMessage("La fecha 'desde' debe ser menor o igual a la fecha 'hasta'")
            .When(x => x.CreatedFrom.HasValue && x.CreatedTo.HasValue);
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy)) return false;

        var validFields = new[]
        {
            "UserId", "Username", "Email", "FullName", "StatusId",
            "CreatedAt", "UpdatedAt", "LastAccessAt"
        };

        return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }
}

public class BlockUserValidator : AbstractValidator<BlockUserDto>
{
    public BlockUserValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("El ID del usuario es requerido");

        RuleFor(x => x.StatusReasonId)
            .GreaterThan(0)
            .WithMessage("La razón del bloqueo es requerida");

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .WithMessage("El comentario no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Comment));
    }
}

public class AssignRolesValidator : AbstractValidator<AssignRolesDto>
{
    public AssignRolesValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("El ID del usuario es requerido");

        RuleFor(x => x.RoleIds)
            .NotEmpty()
            .WithMessage("Debe asignar al menos un rol");

        RuleForEach(x => x.RoleIds)
            .GreaterThan(0)
            .WithMessage("Los IDs de rol deben ser mayores a 0");
    }
}