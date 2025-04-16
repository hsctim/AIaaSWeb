using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AIaaS.Nlp.Dtos
{
    public class GetNlpQAForEditOutput
    {
        public CreateOrEditNlpQADto NlpQA { get; set; }

        public string NlpChatbotName { get; set; }
        public Guid NlpChatbotId { get; set; }
        public string NlpChatbotLanguage { get; set; }

        //public bool EnabledGPT { get; set; }

        //public IList<NlpWorkflowStateDto> ChatbotSelectList { get; set; }

        //public IList<NlpWorkflowStateDto> CurrentWFSSelectList { get; set; }

        //public IList<NlpWorkflowStateDto> NextWFSSelectList { get; set; }
    }
}