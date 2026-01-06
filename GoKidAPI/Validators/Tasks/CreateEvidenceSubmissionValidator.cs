using FluentValidation;

using GoKidAPI.DTO.Tasks.Requests;

namespace GoKidAPI.Validators.Tasks
{
    public class CreateEvidenceSubmissionValidator : AbstractValidator<CreateEvidenceSubmissionRequest>
    {
        public CreateEvidenceSubmissionValidator()
        {
            Include(new CreateBaseTaskValidator<CreateEvidenceSubmissionRequest>());

            RuleFor(x => x.InstructionsText)
                .NotEmpty().WithMessage("Instructions text is required")
                .MaximumLength(1000);

            RuleFor(x => x.EvidenceType)
                .IsInEnum().WithMessage("Invalid evidence type");

            RuleFor(x => x.ReviewBy)
                .IsInEnum().WithMessage("Invalid review authority");
        }
    }
}
