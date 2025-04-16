using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Dto
{
    public class NlpTenantCoreDto
    {
        public int TenantId { get; set; }

        public double NlpPriority { get; set; }

        public double SubscriptionAmountPerYear { get; set; }   //PerYear

        public bool IsFreeEdition { get; set; }
        public bool IsFreeEditionInFirst30Days { get; set; }
        public bool IsPaidEditionExpired { get; set; }
    }
}
