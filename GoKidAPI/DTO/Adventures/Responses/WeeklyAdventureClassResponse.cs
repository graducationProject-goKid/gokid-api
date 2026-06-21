namespace GoKidAPI.DTO.Adventures.Responses
{
    public class WeeklyAdventureClassResponse
    {
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public int ChildrenCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
