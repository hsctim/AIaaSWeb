using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.License
{
    public class LicenseUsage
    {
        public int LicenseCount { get; set; }
        public int UsageCount { get; set; }

        public bool Creatable()
        {
            return UsageCount < LicenseCount;
        }
    }
}
