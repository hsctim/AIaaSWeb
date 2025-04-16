using AIaaS.Nlp.Dtos;
using System.Collections.Generic;

using Abp.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace AIaaS.Web.Areas.App.Models.NlpQAs
{
    public class NlpChatbotSelectionModel
    {

        public SelectList ChatbotSelectList { get; set; }
    }
}