using FluentValidation;
using FeborBack.Application.DTOs.Auth;

namespace FeborBack.Application.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("El refresh token es requerido");
    }
}