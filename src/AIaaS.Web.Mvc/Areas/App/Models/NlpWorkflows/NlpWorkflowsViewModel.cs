using Microsoft.AspNetCore.Mvc.Rendering;

namespace AIaaS.Web.Areas.App.Models.NlpWorkflows
{
    public class NlpWorkflowsViewModel
    {
        //public string FilterText { get; set; }

        //public int ChatbotCount { get; set; }
        public SelectList ChatbotSelectList { get; set; }
    }
}
