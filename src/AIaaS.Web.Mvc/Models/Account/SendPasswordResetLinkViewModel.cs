using System.ComponentModel.DataAnnotations;

namespace AIaaS.Web.Models.Account
{
    public class SendPasswordResetLinkViewModel
    {
        [Required]
        public string EmailAddress { get; set; }
    }
}