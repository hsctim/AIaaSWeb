using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp.Dtos
{
    public class GetNlpWorkflowForEditOutput
    {
        public CreateOrEditNlpWorkflowDto NlpWorkflow { get; set; }


        //public string NlpChatbotName { get; set; }

    }
}