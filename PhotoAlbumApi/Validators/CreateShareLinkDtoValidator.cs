using FluentValidation;
using PhotoAlbumApi.DTOs;

namespace PhotoAlbumApi.Validators;
public class CreateShareLinkDtoValidator : AbstractValidator<CreateShareLinkDto>
{
    public CreateShareLinkDtoValidator()
    {
        RuleFor(x => x.AlbumId)
            .NotEmpty().WithMessage("AlbumId is required.");

        RuleFor(x => x.ExpiresInHours)
            .InclusiveBetween(1, 720).WithMessage("ExpiresInHours must be between 1 and 720 (30 days).");
    }
}
