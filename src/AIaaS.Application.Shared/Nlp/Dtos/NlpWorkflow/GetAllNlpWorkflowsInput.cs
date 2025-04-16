using Abp.Application.Services.Dto;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetAllNlpWorkflowsInput : PagedAndSortedResultRequestDto
    {
        public Guid? NlpChatbotId { get; set; }

    }
}