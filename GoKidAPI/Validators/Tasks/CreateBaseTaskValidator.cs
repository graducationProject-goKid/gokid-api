using FluentValidation;

using GoKidAPI.DTO.Tasks.Requests;

namespace GoKidAPI.Validators.Tasks
{
    public class CreateBaseTaskValidator<T>
        : AbstractValidator<T> where T : CreateBaseTaskRequest
    {
        public CreateBaseTaskValidator()
        {
            RuleFor(x => x.TitleAr)
                .NotEmpty().WithMessage("العنوان بالعربي مطلوب")
                .MaximumLength(100);

            RuleFor(x => x.TitleEn)
                .NotEmpty().WithMessage("Title in English is required")
                .MaximumLength(100);

            RuleFor(x => x.DescriptionAr)
                .NotEmpty().WithMessage("الوصف بالعربي مطلوب")
                .MaximumLength(500);

            RuleFor(x => x.DescriptionEn)
                .NotEmpty().WithMessage("Description in English is required")
                .MaximumLength(500);

            RuleFor(x => x.SubCategoryId)
                .NotEmpty()
                .Must(BeValidGuid).WithMessage("Invalid SubCategoryId format");

            RuleFor(x => x.BasePoints)
                .GreaterThan(0)
                .LessThanOrEqualTo(1000);

            RuleFor(x => x.IconFile)
                .Must(BeValidImage)
                .When(x => x.IconFile != null)
                .WithMessage("Icon must be a valid image (jpg, jpeg, png)");
        }

        private bool BeValidGuid(string guid)
            => Guid.TryParse(guid, out _);

        private bool BeValidImage(IFormFile? file)
        {
            if (file == null) return true;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }
    }
    }
