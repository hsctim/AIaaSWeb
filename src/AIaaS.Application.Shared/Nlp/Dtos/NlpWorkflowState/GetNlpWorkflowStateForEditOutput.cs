using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AIaaS.Nlp.Dtos
{
    public class GetNlpWorkflowStateForEditOutput
    {
        public CreateOrEditNlpWorkflowStateDto NlpWorkflowState { get; set; }

        public string NlpChatbotName { get; set; }

        public string NlpWorkflowName { get; set; }

    }
}