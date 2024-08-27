using FluentValidation;
using PhotoAlbumApi.DTOs;

namespace PhotoAlbumApi.Validators;

public class PhotoUploadDtoValidator : AbstractValidator<PhotoUploadDto>
{
    public PhotoUploadDtoValidator()
    {
        RuleFor(photo => photo.File)
            .NotNull().WithMessage("File must be provided.")
            .Must(file => file.Length > 0).WithMessage("File must be provided.");

        RuleFor(photo => photo.AlbumId)
            .NotEmpty().WithMessage("AlbumId is required.");

        RuleFor(photo => photo.Title)
            .NotEmpty().WithMessage("Title must be provided.")
            .MaximumLength(100).WithMessage("Title cannot exceed 100 characters.");

        RuleFor(photo => photo.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}