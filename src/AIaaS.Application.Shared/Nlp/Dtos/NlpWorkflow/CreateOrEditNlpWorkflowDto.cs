using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp.Dtos
{
    public class CreateOrEditNlpWorkflowDto : EntityDto<Guid?>
    {

        [StringLength(NlpWorkflowConsts.MaxNameLength, MinimumLength = NlpWorkflowConsts.MinNameLength)]
        public string Name { get; set; }

        public bool Disabled { get; set; }

        public Guid NlpChatbotId { get; set; }

    }
}