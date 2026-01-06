using FluentValidation;

using GoKidAPI.Data;
using GoKidAPI.DTO.Category.Requests;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Validators.Category
{
    public class CreateSubCategoryRequestValidator : AbstractValidator<CreateSubCategoryRequest>
    {
        public CreateSubCategoryRequestValidator(AppDbContext context)
        {
            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("Arabic name is required")
                .MinimumLength(2);

            RuleFor(x => x.NameEn)
                .NotEmpty().WithMessage("English name is required")
                .MinimumLength(2)
                .Matches("^[a-zA-Z0-9\\s&-]+$").WithMessage("English name can only contain letters, numbers, spaces, & and -");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required")
                .MustAsync(async (id, ct) => await context.TaskCategories.AnyAsync(c => c.Id == id, ct))
                .WithMessage("Invalid CategoryId");

            RuleFor(x => x.IconFile)
                .NotNull().WithMessage("Icon is required")
                .Must(file => file.Length > 0).WithMessage("Icon file is empty")
                .Must(file => file.Length <= 5 * 1024 * 1024).WithMessage("Icon size must be less than 5MB")
                .Must(file => new[] { "image/jpeg", "image/png", "image/svg+xml" }.Contains(file.ContentType))
                .WithMessage("Only JPG, PNG and SVG are allowed");

            // Async: Unique name inside same category
            //RuleFor(x => x)
            //    .MustAsync(async (req, ct) =>
            //    {
            //        return 
                    
            //        !await context.SubCategories.AnyAsync(s =>
            //            s.CategoryId == req.CategoryId &&
            //            (s.NameAr == req.NameAr || s.NameEn.ToLower() == req.NameEn.ToLower()), ct);
            //    })
            //    .WithMessage("SubCategory with this name already exists in this category");
        }
    }
}
