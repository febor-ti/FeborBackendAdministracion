using FluentValidation;
using FeborBack.Application.DTOs.Auth;

namespace FeborBack.Application.Validators;

public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(255)
            .WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"[A-Z]")
            .WithMessage("La contraseña debe contener al menos una letra mayúscula")
            .Matches(@"[a-z]")
            .WithMessage("La contraseña debe contener al menos una letra minúscula")
            .Matches(@"\d")
            .WithMessage("La contraseña debe contener al menos un número")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")
            .WithMessage("La contraseña debe contener al menos un carácter especial");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("El nombre completo es requerido")
            .MinimumLength(2)
            .WithMessage("El nombre completo debe tener al menos 2 caracteres")
            .MaximumLength(255)
            .WithMessage("El nombre completo no puede exceder 255 caracteres");

        RuleForEach(x => x.RoleIds)
            .GreaterThan(0)
            .WithMessage("Los IDs de rol deben ser mayores a 0");
    }
}