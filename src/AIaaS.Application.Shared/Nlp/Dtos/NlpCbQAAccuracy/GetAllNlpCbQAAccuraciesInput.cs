using Abp.Application.Services.Dto;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetAllNlpCbQAAccuraciesInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }


        public DateTime? MaxNlpCreationTimeFilter { get; set; }
        public DateTime? MinNlpCreationTimeFilter { get; set; }

        public Guid? NlpChatbotId { get; set; }

    }
}