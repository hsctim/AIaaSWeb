using Abp.Application.Services.Dto;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetAllNlpQAsInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

        public string CategoryFilter { get; set; }

        //public string QuestionFilter { get; set; }

        //public string AnswerFilter { get; set; }

        public string NlpChatbotGuidFilter { get; set; }
    }
}