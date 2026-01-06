using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Requests
{
    public class CreateEvidenceSubmissionRequest: CreateBaseTaskRequest
    {
        public string InstructionsText { get; set; } = null!;
        public EvidenceType EvidenceType { get; set; } = EvidenceType.Image;
        public ReviewAuthority ReviewBy { get; set; } = ReviewAuthority.Parent;
    }
}
