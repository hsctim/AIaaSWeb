using System.ComponentModel.DataAnnotations;

namespace AIaaS.Authorization.Users.Dto
{
    public class ChangeUserLanguageDto
    {
        [Required]
        public string LanguageName { get; set; }
    }
}
