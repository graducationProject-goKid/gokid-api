namespace GoKidAPI.DTO.Classes.Responses
{
    public class ClassDetailsResponse
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string InstitutionName { get; set; } = null!;
        public int ChildrenCount { get; set; }
        public int SupervisorsCount { get; set; }
        public int AdventuresCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
