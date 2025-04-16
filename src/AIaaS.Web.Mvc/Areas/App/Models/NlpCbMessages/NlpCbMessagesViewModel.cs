using Microsoft.AspNetCore.Mvc.Rendering;

namespace AIaaS.Web.Areas.App.Models.NlpCbMessages
{
    public class NlpCbMessagesViewModel
    {
        public string FilterText { get; set; }
        public SelectList ChatbotSelectList { get; set; }

    }
}