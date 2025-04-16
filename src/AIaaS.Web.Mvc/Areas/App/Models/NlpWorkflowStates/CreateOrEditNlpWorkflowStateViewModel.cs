using AIaaS.Nlp.Dtos;
using System.Collections.Generic;

using Abp.Extensions;
using System;

namespace AIaaS.Web.Areas.App.Models.NlpWorkflowStates
{
    public class CreateOrEditNlpWorkflowStateModalViewModel
    {
        public CreateOrEditNlpWorkflowStateDto NlpWorkflowState { get; set; }

        public string NlpChatbotName { get; set; }
        public string NlpWorkflowName { get; set; }
        public bool IsEditMode => NlpWorkflowState.Id.HasValue;

        public NlpWfsFalsePredictionOpDto FalsePrediction1_Op;
        public NlpWfsFalsePredictionOpDto FalsePrediction3_Op;

        public List<NlpLookupTableDto> WorkflowStateList { get; set; }

        public bool IsViewMode = true;
    }
}