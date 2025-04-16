using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpWorkflowDto : EntityDto<Guid>
    {
        public string Name { get; set; }

        public bool Disabled { get; set; }

        public Guid NlpChatbotId { get; set; }

    }
}