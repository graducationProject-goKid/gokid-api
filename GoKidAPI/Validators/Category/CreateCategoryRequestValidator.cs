using FluentValidation;

using GoKidAPI.Data;
using GoKidAPI.DTO.Category.Requests;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Validators.Category
{
    public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
    {
        public CreateCategoryRequestValidator(AppDbContext context)
        {
            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("Arabic name is required")
                .MinimumLength(2).WithMessage("Arabic name must be at least 2 characters");

            RuleFor(x => x.NameEn)
                .NotEmpty().WithMessage("English name is required")
                .MinimumLength(2).WithMessage("English name must be at least 2 characters")
                .Matches("^[a-zA-Z0-9\\s&-]+$").WithMessage("English name can only contain letters, numbers, spaces, & and -");

            RuleFor(x => x.ColorHex)
                .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").WithMessage("Invalid hex color format")
                .When(x => !string.IsNullOrEmpty(x.ColorHex));

            // Async: Check name uniqueness
            //RuleFor(x => x)
            //    .MustAsync(async (req, ct) =>
            //    {
            //        return !await context.TaskCategories.AnyAsync(c =>
            //            (c.NameAr == req.NameAr || c.NameEn.ToLower() == req.NameEn.ToLower()), ct);
            //    })
            //    .WithMessage("Category with this Arabic or English name already exists");
        }
    }
}
