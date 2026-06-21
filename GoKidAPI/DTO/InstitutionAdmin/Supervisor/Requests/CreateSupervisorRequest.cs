namespace GoKidAPI.DTO.InstitutionAdmin.Supervisor.Requests
{
    public class CreateSupervisorRequest
    {
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public IFormFile? AvatarFile { get; set; }
    }
}
