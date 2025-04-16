namespace AIaaS.Web.Models.Account
{
    public class LoginViewModel : LoginModel
    {
        public bool RememberMe { get; set; }

        public bool isHost { get; set; }
        public int? TenantId { get; set; }
    }
}