using FluentValidation;

using GoKidAPI.Data;
using GoKidAPI.DTO.Category.Requests;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Validators.Category
{
    public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
    {
        public UpdateCategoryRequestValidator(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            RuleFor(x => x.NameAr)
                .MinimumLength(2).When(x => !string.IsNullOrEmpty(x.NameAr))
                .WithMessage("Arabic name must be at least 2 characters");

            RuleFor(x => x.NameEn)
                .MinimumLength(2).When(x => !string.IsNullOrEmpty(x.NameEn))
                .Matches("^[a-zA-Z0-9\\s&-]+$").When(x => !string.IsNullOrEmpty(x.NameEn))
                .WithMessage("English name can only contain letters, numbers, spaces, & and -");

            RuleFor(x => x.ColorHex)
                .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").When(x => !string.IsNullOrEmpty(x.ColorHex))
                .WithMessage("Invalid hex color format");

            //// Async: Prevent duplicate name (except current category)
            //var subCategoryId = httpContextAccessor.HttpContext?
            //            .Request?
            //            .RouteValues["id"]?
            //            .ToString();

            //RuleFor(x => x)
            //    .MustAsync(async (req, ct) =>
            //    {
            //        if (string.IsNullOrEmpty(req.NameAr) && string.IsNullOrEmpty(req.NameEn))
            //            return true;

            //        var query = context.TaskCategories.AsQueryable();

            //        if (!string.IsNullOrEmpty(subCategoryId))
            //            query = query.Where(c => c.Id != subCategoryId);

            //        return !await query.AnyAsync(c =>
            //            (req.NameAr != null && c.NameAr == req.NameAr) ||
            //            (req.NameEn != null && c.NameEn.ToLower() == req.NameEn.ToLower()), ct);
            //    })
            //    .WithMessage("Another category with this name already exists");
        }
    }
}
