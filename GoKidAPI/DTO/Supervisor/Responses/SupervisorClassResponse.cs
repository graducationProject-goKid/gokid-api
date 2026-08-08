namespace GoKidAPI.DTO.Supervisor.Responses
{
    public class SupervisorClassResponse
    {
        public string ClassId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string InstitutionName { get; set; } = null!;
        public int ChildrenCount { get; set; }
        public int ActiveAdventuresCount { get; set; }
    }
}
