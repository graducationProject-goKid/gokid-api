using FluentValidation;

using GoKidAPI.Data;
using GoKidAPI.DTO.Account.Auth.Requests;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Validators.Auth
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator(UserManager<AppUser> userManager, AppDbContext context)
        {
            RuleFor(x => x.Identifier).NotEmpty().WithMessage("Identifier is required");

            RuleFor(x => x.LoginAs).IsInEnum().WithMessage("Invalid user type");

            // Parent / Staff → Email + Password
            When(x => x.LoginAs == UserType.Parent ||
                x.LoginAs == UserType.InstitutionAdmin ||
                x.LoginAs == UserType.Supervisor ||
                x.LoginAs == UserType.PlatformAdmin, () =>
                {
                    RuleFor(x => x.Identifier)
                    .EmailAddress().WithMessage("Invalid email format");

                    RuleFor(x => x.Password)
                    .NotEmpty().WithMessage("Password is required");

                    RuleFor(x => x)
                    .MustAsync(async (req, ct) =>
                    {
                        var user = await userManager.FindByEmailAsync(req.Identifier);
                        if (user == null) return false;
                        if (user.UserType != req.LoginAs) return false;
                        if (!user.EmailConfirmed) return false;
                        return await userManager.CheckPasswordAsync(user, req.Password!);
                    })
                    .WithMessage("Invalid credentials or unauthorized role");
                }
            );

            // Child → Code only
            When(x => x.LoginAs == UserType.Child, () =>
            {
                RuleFor(x => x.Identifier)
                    .Length(6).WithMessage("Child code must be 6 digits")
                    .Matches(@"^\d{6}$").WithMessage("Child code must be digits only");

                RuleFor(x => x.Identifier)
                    .MustAsync(async (code, ct) =>
                        await context.Childrens.AnyAsync(c => c.RegistrationCode == code))
                    .WithMessage("Invalid or already used child code");

                RuleFor(x => x.Password).Null().WithMessage("Password should not be sent for child login");
            });
        }
    }
}
