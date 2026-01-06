using FluentValidation;

using GoKidAPI.Data;
using GoKidAPI.DTO.Category.Requests;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Validators.Category
{
    public class UpdateSubCategoryRequestValidator : AbstractValidator<UpdateSubCategoryRequest>
    {
        public UpdateSubCategoryRequestValidator(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            var subCategoryId = httpContextAccessor.HttpContext ?
                        .Request?
                        .RouteValues["id"]?
                        .ToString();

            RuleFor(x => x.NameAr)
                .MinimumLength(2).When(x => !string.IsNullOrEmpty(x.NameAr));

            RuleFor(x => x.NameEn)
                .MinimumLength(2).When(x => !string.IsNullOrEmpty(x.NameEn))
                .Matches("^[a-zA-Z0-9\\s&-]+$").When(x => !string.IsNullOrEmpty(x.NameEn));

            RuleFor(x => x.IconFile)
                .Must(file => file.Length <= 5 * 1024 * 1024).When(x => x.IconFile != null)
                .WithMessage("Icon size must be less than 5MB")
                .Must(file => new[] { "image/jpeg", "image/png", "image/svg+xml" }.Contains(file?.ContentType))
                .When(x => x.IconFile != null);

            //// Async: Prevent duplicate name in same category
            //RuleFor(x => x)
            //    .MustAsync(async (req, ct) =>
            //    {
            //        if (string.IsNullOrEmpty(req.NameAr) && string.IsNullOrEmpty(req.NameEn))
            //            return true;

            //        // Get current subcategory category if not provided
            //        var categoryId = req.CategoryId;

            //        if (categoryId == null)
            //        {
            //            var current = await context.SubCategories
            //                .AsNoTracking()
            //                .FirstOrDefaultAsync(s => s.CategoryId == req.CategoryId);

            //            if (current == null)
            //                return true;

            //            categoryId = current.CategoryId;
            //        }

            //        var exists = await context.SubCategories
            //            .AnyAsync(s =>
            //                s.Id != id &&
            //                s.CategoryId == categoryId &&
            //                (
            //                    (!string.IsNullOrEmpty(req.NameAr) && s.NameAr == req.NameAr) ||
            //                    (!string.IsNullOrEmpty(req.NameEn) && s.NameEn.ToLower() == req.NameEn.ToLower())
            //                ),
            //                ct
            //            );

            //        return !exists;
            //    })
            //    .WithMessage("Another subcategory with this name already exists in the same category");
        }
    }
}
