using AIaaS.Nlp.Dtos;
using System.Collections.Generic;

using Abp.Extensions;

namespace AIaaS.Web.Areas.App.Models.NlpWorkflows
{
    public class CreateOrEditNlpWorkflowModalViewModel
    {
        public CreateOrEditNlpWorkflowDto NlpWorkflow { get; set; }

        //public string NlpChatbotName { get; set; }

        public List<NlpLookupTableDto> NlpWorkflowNlpChatbotList { get; set; }

        public bool IsEditMode => NlpWorkflow.Id.HasValue;

        public NlpChatbotDto NlpChatbot { get; set; }

        public bool IsViewMode = true;
    }
}
