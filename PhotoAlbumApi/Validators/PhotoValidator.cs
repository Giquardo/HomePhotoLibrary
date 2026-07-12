using FluentValidation;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.DTOs;

namespace PhotoAlbumApi.Validators;

public class PhotoValidator : AbstractValidator<PhotoDto>
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

        RuleFor(photo => photo.Url)
            .NotEmpty()
            .WithMessage("URL must be provided.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("URL must be a well-formed absolute http or https URI.");
    }
}