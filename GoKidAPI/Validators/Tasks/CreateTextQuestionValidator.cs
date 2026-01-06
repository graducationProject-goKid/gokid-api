using FluentValidation;

using GoKidAPI.DTO.Tasks.Requests;

namespace GoKidAPI.Validators.Tasks
{
    public class CreateTextQuestionValidator : AbstractValidator<CreateTextQuestionRequest>
    {
        public CreateTextQuestionValidator()
        {
            Include(new CreateBaseTaskValidator<CreateTextQuestionRequest>());

            RuleFor(x => x.QuestionText)
                .NotEmpty().WithMessage("Question text is required")
                .MaximumLength(500).WithMessage("Question text must not exceed 500 characters");

            RuleFor(x => x.ExpectedCorrectAnswer)
                .NotEmpty().WithMessage("Expected correct answer is required")
                .MaximumLength(200);

            RuleFor(x => x.TaskImageFile)
                .Must(BeValidImage).When(x => x.TaskImageFile != null)
                .WithMessage("Task image must be a valid image format");
        }

        private bool BeValidImage(IFormFile? file)
        {
            if (file == null) return true;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }
    }

}
