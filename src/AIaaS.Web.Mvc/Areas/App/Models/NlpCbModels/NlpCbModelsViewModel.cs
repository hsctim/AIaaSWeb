using Microsoft.AspNetCore.Mvc.Rendering;

namespace AIaaS.Web.Areas.App.Models.NlpCbModels
{
    public class NlpCbModelsViewModel
    {
		public string FilterText { get; set; }

        public string ChatbotId { get; set; }
        public string ChatbotName { get; set; }

        public SelectList ChatbotSelectList { get; set; }

    }
}