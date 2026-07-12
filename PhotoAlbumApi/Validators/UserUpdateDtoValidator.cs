using FluentValidation;
using PhotoAlbumApi.DTOs;

namespace PhotoAlbumApi.Validators;
public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateDtoValidator()
    {
        RuleFor(user => user.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50).WithMessage("Username must be at most 50 characters long.");

        RuleFor(user => user.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(50).WithMessage("Email must be at most 50 characters long.");

        // Only validated when a new password is actually supplied - blank means
        // "keep the existing password."
        RuleFor(user => user.Password)
            .MinimumLength(12).WithMessage("Password must be at least 12 characters long.")
            .MaximumLength(72).WithMessage("Password must be at most 72 characters long.")
            .When(user => !string.IsNullOrEmpty(user.Password));

        RuleFor(user => user.IsAdmin)
            .NotNull().WithMessage("IsAdmin is required.");
    }
}
