namespace GoKidAPI.DTO.Institution.Requests
{
    public class UpdateInstitutionRequest
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }
        public IFormFile? Logo { get; set; }
    }
}
