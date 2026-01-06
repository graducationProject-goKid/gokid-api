using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Requests
{
    public class CreateTextQuestionRequest : CreateBaseTaskRequest
    {
        public string QuestionText { get; set; } = null!;
        public string ExpectedCorrectAnswer { get; set; } = null!;
        public bool CaseSensitive { get; set; } = false;
    }
}
