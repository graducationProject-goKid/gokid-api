using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Base;
using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.Entity.Institiution
{
    public class Adventure : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;

        public string? BannerImageUrl { get; set; }
        public string? BannerImagePublicId { get; set; }

        public string DescriptionEn { get; set; } = null!; // Description is the adventure mode so it may be = drama or action or comedy
        public string DescriptionAr { get; set; } = null!;

        public string GoalEn { get; set; } = null!;
        public string GoalAr { get; set; } = null!;

        public string? DescriptionVoiceUrl { get; set; } // Description voice is equal to = intro voice 
        public string? DescriptionVoicePublicId { get; set; }

        // For the Adventure Intro and Outro, we can have optional titles, stories, and voice URLs.
        // That created when the admin adding the adventure
        public string? IntroTitle { get; set; }
        public string? IntroStory { get; set; }
        public string? IntroVoiceUrl { get; set; }
        public string? IntroVoicePublicId { get; set; }

        public string? OutroTitle { get; set; }
        public string? OutroStory { get; set; }
        public string? OutroVoiceUrl { get; set; }
        public string? OutroVoicePublicId { get; set; }

        public int WeekDuration { get; set; } = 7;

        public int BonusPoints { get; set; } = 50;

        public AdventureStatus Status { get; set; } = AdventureStatus.Inactive;
        
        [ForeignKey(nameof(InstitutionId))]
        public string InstitutionId { get; set; } = null!;
        public Institution Institution { get; set; } = null!;

        public ICollection<AdventureTask> Tasks { get; set; } = new List<AdventureTask>();

        public ICollection<WeeklyAdventure> WeeklyAssignments { get; set; } = new List<WeeklyAdventure>();
    }
}
