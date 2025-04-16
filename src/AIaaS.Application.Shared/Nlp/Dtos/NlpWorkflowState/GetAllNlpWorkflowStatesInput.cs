using Abp.Application.Services.Dto;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetAllNlpWorkflowStatesInput : PagedAndSortedResultRequestDto
    {
        public Guid NlpWorkflowId { get; set; }
    }
}
