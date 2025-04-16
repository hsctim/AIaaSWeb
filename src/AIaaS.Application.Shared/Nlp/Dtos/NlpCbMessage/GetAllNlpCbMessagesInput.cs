using Abp.Application.Services.Dto;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetAllNlpCbMessagesInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

        public DateTime? MaxNlpSentTimeFilter { get; set; }
        public DateTime? MinNlpSentTimeFilter { get; set; }

        public Guid? NlpChatbotId { get; set; }

        //public string NlpChatbotGuidFilter { get; set; }

        //public string NlpChatbotNameFilter { get; set; }
    }
}