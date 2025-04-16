using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpCbTrainedAnswerDto : EntityDto<Guid>
    {
        public Guid NlpCbTrainingDataId { get; set; }
        public int NNID { get; set; }
    }
}