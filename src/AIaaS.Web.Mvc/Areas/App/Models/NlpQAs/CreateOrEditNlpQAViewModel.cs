using AIaaS.Nlp.Dtos;
using System.Collections.Generic;

using Abp.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace AIaaS.Web.Areas.App.Models.NlpQAs
{
    public class CreateOrEditNlpQAModalViewModel
    {
        public CreateOrEditNlpQADto NlpQA { get; set; }

        public string NlpChatbotName { get; set; }

        public bool IsEditMode => NlpQA.Id.HasValue;

        public Guid NlpChatbotId { get; set; }

        public string NlpChatbotLanguage { get; set; }

        public List<NlpLookupTableDto> ChatbotSelectList { get; set; }

        public List<NlpLookupTableDto> CurrentWFSSelectList { get; set; }

        public List<NlpLookupTableDto> NextWFSSelectList { get; set; }

        public bool IsViewMode = true;
    }
}