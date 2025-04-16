using Abp.Application.Services.Dto;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetAllNlpCbModelsInput : PagedAndSortedResultRequestDto
    {
        //public string Filter { get; set; }

        //public string NlpChatbotGuidFilter { get; set; }
        public Guid? NlpChatbotId { get; set; }

    }
}