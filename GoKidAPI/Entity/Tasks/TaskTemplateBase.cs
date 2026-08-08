using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums.Tasks;

// AutoCompletedTasks  Press start the task and then only done and he will take the task points (encorugment tasks)
// QuestionedTasks has (Image - Question on image - ExpectedCorrectAnswer - Text Answer)
// VoiceTasks          (Image - Question on image - ExpectedCorrectAnswer - Voice Answer - ExpectedCorrectAnswer)
// ReviweTasks         (Image - Question or what we need child to do - Child will upload image of the task(sended to parent to verfiy or send to platofrm to verify based on task type)
public class TaskTemplateBase : AuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TitleAr { get; set; } = null!;
    public string TitleEn { set; get; } = null!;
    public string DescriptionAr { get; set; } = null!;
    public string DescriptionEn { get; set; } = null!;
    public string? TaskImageUrl { get; set; }
    public string? TaskImagePublicId { get; set; }
    public string? IconUrl { get; set; }
    public string? IconPublicId { get; set; }

    [ForeignKey(nameof(SubCategoryId))]
    public string? SubCategoryId { get; set; }
    public TaskSubCategory? SubCategory { get; set; }

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Easy;
    public TaskTemplateType TemplateType { get; set; }

    public int BasePoints { get; set; } = 10;

    // Recommended age range for this task (e.g. 5-7 years). Used to suggest age-appropriate
    // tasks when assigning to a child or building an Adventure for a class.
    public int? RecommendedAgeFrom { get; set; }
    public int? RecommendedAgeTo { get; set; }

    // EvidenceSubmissionTask
    public string? InstructionsText { get; set; } = null!;
    public EvidenceType? EvidenceType { get; set; } // Future: Video, Document, Audio
    public ReviewAuthority? ReviewBy { get; set; } = ReviewAuthority.Parent; // Future: Platform

    // TextQuestionTask
    public string? QuestionText { get; set; } = null!;
    public string? ExpectedCorrectAnswer { get; set; } = null!;
    public bool? CaseSensitive { get; set; } = false;

    // Voice 
    public string? VoiceQuestionText { get; set; } = null!;
    public string? VoiceExpectedCorrectAnswer { get; set; } = null!;
    public string? VoicePrompt { get; set; }
    public int? MaxVoiceAttempts { get; set; } = 3;
    public int? MaxVoiceDurationSeconds { get; set; } = 10;

    public ICollection<ChildTask>? ChildTasks { get; set; }
    public ICollection<AdventureTask>? AdventureTasks { get; set; }
}