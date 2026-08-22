using BooksProject.Dtos;
using FluentValidation;

namespace BooksProject.Validators;

public class SignupValidator : AbstractValidator<SignupDto>
{
    public SignupValidator()
    {
        RuleFor(x => x.name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain a number.");

        RuleFor(x => x.confirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");
    }
}