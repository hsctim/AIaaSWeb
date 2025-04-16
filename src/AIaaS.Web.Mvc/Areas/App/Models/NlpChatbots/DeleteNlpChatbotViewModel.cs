using AIaaS.Nlp.Dtos;

using Abp.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;

namespace AIaaS.Web.Areas.App.Models.NlpChatbots
{
    public class DeleteNlpChatbotModalViewModel
    {
        public Guid ChatbotId { get; set; }

        public string ChatbotName { get; set; }

        public string UserEmail { get; set; }
    }
}