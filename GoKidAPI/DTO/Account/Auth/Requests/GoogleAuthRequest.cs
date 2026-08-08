using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Account.Auth.Requests
{
    public class GoogleAuthRequest
    {
        /// <summary>Google ID token obtained from the Google Sign-In SDK on the client</summary>
        [Required]
        public string IdToken { get; set; } = null!;
    }
}
