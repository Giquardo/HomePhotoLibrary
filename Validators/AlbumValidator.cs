using FluentValidation;
using PhotoAlbumApi.DTOs;

namespace PhotoAlbumApi.Validators;

public class AlbumValidator : AbstractValidator<AlbumDto>
{
    public AlbumValidator()
    {
        RuleFor(album => album.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(album => album.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(album => !string.IsNullOrEmpty(album.Description)); // Ensure Description is optional
    }
}