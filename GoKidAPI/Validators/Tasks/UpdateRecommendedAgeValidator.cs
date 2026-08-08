using FluentValidation;

using GoKidAPI.DTO.Tasks.Requests;

namespace GoKidAPI.Validators.Tasks
{
    public class UpdateRecommendedAgeValidator : AbstractValidator<UpdateRecommendedAgeRequest>
    {
        public UpdateRecommendedAgeValidator()
        {
            RuleFor(x => x.RecommendedAgeFrom)
                .InclusiveBetween(1, 18).WithMessage("Recommended age from must be between 1 and 18");

            RuleFor(x => x.RecommendedAgeTo)
                .InclusiveBetween(1, 18).WithMessage("Recommended age to must be between 1 and 18")
                .GreaterThanOrEqualTo(x => x.RecommendedAgeFrom)
                .WithMessage("Recommended age to must be greater than or equal to recommended age from");
        }
    }
}
