namespace GoKidAPI.DTO.Adventures.Responses
{
    public class CreateAdventureResponse
    {
        public string AdventureId { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string DescriptionVoiceUrl { get; set; } = null!;
        public int TasksCount { get; set; }
        public bool VoiceProcessingInBackground {get; set;}
    }
}
