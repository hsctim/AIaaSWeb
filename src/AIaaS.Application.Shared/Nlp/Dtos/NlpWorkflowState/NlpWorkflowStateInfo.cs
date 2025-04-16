using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpWorkflowStateInfo : NlpWorkflowStateDto
    {
        public string NlpWorkflowName { get; set; }

    }
}