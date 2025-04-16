using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Tenants.Dashboard.Dto
{
    public class GetSubscriptionSummaryOutput
    {
        public string TenantCodeName { get; set; }
        public string EditionName { get; set; }
        public DateTime? SubscriptionEndDateUtc { get; set; }
        public bool IsFree { get; set; }


        public int MaxUserCount { get; set; }
        public int CurrentUserCount { get; set; }
        public int MaxChatbotCount { get; set; }
        public int CurrentChatbotCount { get; set; }
        public int MaxQACount { get; set; }
        //public int CurrentQACount { get; set; }
        public int MaxPUCount { get; set; }



    }
}
