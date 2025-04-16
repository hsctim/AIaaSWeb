using AIaaS.License;

namespace AIaaS.Web.Areas.App.Models.NlpChatbots
{
    public class NlpChatbotsViewModel
    {
        public string FilterText { get; set; }

        //public int ChatbotCount { get; set; }

        public string WarningMessage { get; set; }

        public LicenseUsage Usage { get; set; }
    }
}