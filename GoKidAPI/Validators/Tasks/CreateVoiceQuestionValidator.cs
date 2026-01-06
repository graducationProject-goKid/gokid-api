using FluentValidation;

using GoKidAPI.DTO.Tasks.Requests;

namespace GoKidAPI.Validators.Tasks
{
    public class CreateVoiceQuestionValidator : AbstractValidator<CreateVoiceQuestionRequest>
    {
        public CreateVoiceQuestionValidator()
        {
            Include(new CreateBaseTaskValidator<CreateVoiceQuestionRequest>());

            RuleFor(x => x.QuestionText)
                .NotEmpty().WithMessage("Question text is required")
                .MaximumLength(500);

            RuleFor(x => x.ExpectedCorrectAnswer)
                .NotEmpty().WithMessage("Expected correct answer is required")
                .MaximumLength(200);

            RuleFor(x => x.MaxVoiceAttempts)
                .GreaterThan(0).WithMessage("Max voice attempts must be greater than 0")
                .LessThanOrEqualTo(10).WithMessage("Max voice attempts cannot exceed 10");

            RuleFor(x => x.MaxVoiceDurationSeconds)
                .GreaterThan(0).WithMessage("Max voice duration must be greater than 0")
                .LessThanOrEqualTo(300).WithMessage("Max voice duration cannot exceed 5 minutes");

            RuleFor(x => x.VoicePrompt)
                .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.VoicePrompt));
        }
    }
}
