
using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpCbTrainingDataDto : EntityDto<Guid>
    {

		 public Guid NlpChatbotId { get; set; }

		 
    }
}