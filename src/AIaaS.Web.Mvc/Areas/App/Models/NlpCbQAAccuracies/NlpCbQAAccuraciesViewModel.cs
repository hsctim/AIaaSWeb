using Microsoft.AspNetCore.Mvc.Rendering;

namespace AIaaS.Web.Areas.App.Models.NlpCbQAAccuracies
{
    public class NlpCbQAAccuraciesViewModel
    {
        public string FilterText { get; set; }

        //public string ChatbotId { get; set; }
        //public string ChatbotName { get; set; }

        public SelectList ChatbotSelectList { get; set; }
    }
}