using AIaaS.License;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIaaS.Web.Areas.App.Models.NlpQAs
{
    public class NlpQAsViewModel
    {
        public string FilterText { get; set; }

        //public string ChatbotId { get; set; }
        //public string ChatbotName { get; set; }

        public SelectList ChatbotSelectList { get; set; }

        public int QaCount { get; set; }

        //public string WarningMessage { get; set; }

        //public LicenseUsage Usage { get; set; }
    }
}