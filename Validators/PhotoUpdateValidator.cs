using FluentValidation;

public class PhotoUpdateDtoValidator : AbstractValidator<PhotoUpdateDto>
{
    public PhotoUpdateDtoValidator()
    {
        RuleFor(photo => photo.AlbumId)
            .GreaterThan(0).When(photo => photo.AlbumId != default).WithMessage("AlbumId must be greater than 0.");

        RuleFor(photo => photo.Title)
            .NotEmpty().When(photo => !string.IsNullOrEmpty(photo.Title)).WithMessage("Title is required.")
            .MaximumLength(100).When(photo => !string.IsNullOrEmpty(photo.Title)).WithMessage("Title must not exceed 100 characters.");

        RuleFor(photo => photo.Description)
            .NotEmpty().When(photo => !string.IsNullOrEmpty(photo.Description)).WithMessage("Description is required.")
            .MaximumLength(500).When(photo => !string.IsNullOrEmpty(photo.Description)).WithMessage("Description must not exceed 500 characters.");
    }
}