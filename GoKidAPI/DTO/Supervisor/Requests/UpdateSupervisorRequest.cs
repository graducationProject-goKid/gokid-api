namespace GoKidAPI.DTO.Supervisor.Requests
{
    public class UpdateSupervisorRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public IFormFile? AvatarFile { get; set; }
    }
}
