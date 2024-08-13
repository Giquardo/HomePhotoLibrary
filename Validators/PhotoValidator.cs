using FluentValidation;
using PhotoAlbumApi.Models;

namespace PhotoAlbumApi.Validators;

public class PhotoValidator : AbstractValidator<Photo>
{
    public PhotoValidator()
    {
        RuleFor(photo => photo.AlbumId)
            .NotEmpty().WithMessage("AlbumId is required.");

        RuleFor(photo => photo.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(photo => photo.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}