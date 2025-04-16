using AIaaS.Nlp.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Cache
{
    public class NlpPolicyItem
    {
        public NlpPolicyItem(int maximumCount, int currentCount, NlpTenantCoreDto nlpTenant)
        {
            MaximumCount = maximumCount;
            CurrentCount = currentCount;
            NlpTenant = nlpTenant;
        }

        public int CurrentCount { get; set; }
        public int MaximumCount { get; set; }
        public NlpTenantCoreDto NlpTenant { get; set; }
    }
}
