using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpCbModelDto : EntityDto<Guid>
    {
        //public string NlpCbMLanguage { get; set; }

        public int NlpCbMStatus { get; set; }

        public DateTime? NlpCbMTrainingStartTime { get; set; }

        public DateTime? NlpCbMTrainingCompleteTime { get; set; }

        public DateTime? NlpCbMTrainingCancellationTime { get; set; }

        public double? NlpCbAccuracy { get; set; }

        //public Guid NlpChatbotId { get; set; }

        //public long? NlpCbMTrainingCancellationUserId { get; set; }
    }
}