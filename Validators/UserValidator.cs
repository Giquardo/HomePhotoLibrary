using FluentValidation;
using PhotoAlbumApi.DTOs;

namespace PhotoAlbumApi.Validators;
public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        RuleFor(user => user.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50).WithMessage("Username must be at most 50 characters long.");

        RuleFor(user => user.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(50).WithMessage("Email must be at most 50 characters long.");

        RuleFor(user => user.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            .MaximumLength(50).WithMessage("Password must be at most 50 characters long.");

        RuleFor(user => user.IsAdmin)
            .NotNull().WithMessage("IsAdmin is required.")
            .Must(value => value == true || value == false).WithMessage("IsAdmin must be a boolean value.");
    }
}
