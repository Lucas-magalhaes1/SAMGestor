using FluentValidation;

namespace SAMGestor.Application.Features.Users.UploadPhoto;

public sealed class UploadUserPhotoValidator : AbstractValidator<UploadUserPhotoCommand>
{
    public UploadUserPhotoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID do usuário é obrigatório");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("Arquivo é obrigatório");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("Arquivo vazio")
            .LessThanOrEqualTo(2 * 1024 * 1024).WithMessage("Arquivo muito grande (máx 2MB)");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Nome do arquivo é obrigatório")
            .Must(name => new[] { ".jpg", ".jpeg", ".png" }.Any(ext => 
                name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Formato inválido. Use JPG ou PNG");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type é obrigatório")
            .Must(ct => new[] { "image/jpeg", "image/png" }.Contains(ct.ToLowerInvariant()))
            .WithMessage("Content type inválido");
    }
}