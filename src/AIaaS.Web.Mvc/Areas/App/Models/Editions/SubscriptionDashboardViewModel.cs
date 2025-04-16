using AIaaS.Sessions.Dto;

namespace AIaaS.Web.Areas.App.Models.Editions
{
    public class SubscriptionDashboardViewModel
    {
        public GetCurrentLoginInformationsOutput LoginInformations { get; set; }

        public string EditionDisplayName { get; set; }

        public int UserCount { get; set; }
        public int ChatbotCount { get; set; }
        public int QaCount { get; set; }
        public int ProcessingUnitCount { get; set; }

    }
}
