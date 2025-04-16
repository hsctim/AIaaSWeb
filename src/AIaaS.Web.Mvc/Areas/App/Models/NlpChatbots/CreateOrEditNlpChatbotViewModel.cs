using AIaaS.Nlp.Dtos;

using Abp.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace AIaaS.Web.Areas.App.Models.NlpChatbots
{
    public class CreateOrEditNlpChatbotModalViewModel
    {
        public CreateOrEditNlpChatbotDto NlpChatbot { get; set; }

        public List<string> PictureList { get; set; }

        public SelectList LanguageSelectList { get; set; }

        public SelectList GPTOptionsList { get; set; }

        public bool IsEditMode => NlpChatbot.Id.HasValue;

        public bool IsViewMode = true;
    }
}