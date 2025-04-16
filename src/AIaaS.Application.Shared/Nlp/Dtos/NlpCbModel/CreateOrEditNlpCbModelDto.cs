using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp.Dtos
{
    public class CreateOrEditNlpCbModelDto : EntityDto<Guid?>
    {
        [StringLength(NlpCbModelConsts.MaxNlpCbMLanguageLength, MinimumLength = NlpCbModelConsts.MinNlpCbMLanguageLength)]
        public string NlpCbMLanguage { get; set; }

        public int NlpCbMStatus { get; set; }

        [StringLength(NlpCbModelConsts.MaxNlpCbMInfoLength, MinimumLength = NlpCbModelConsts.MinNlpCbMInfoLength)]
        public string NlpCbMInfo { get; set; }

        public DateTime? NlpCbMTrainingStartTime { get; set; }

        public DateTime? NlpCbMTrainingCompleteTime { get; set; }

        public DateTime? NlpCbMTrainingCancellationTime { get; set; }

        public Guid? NlpChatbotId { get; set; }

        public long? NlpCbMTrainingCancellationUserId { get; set; }

        public long? NlpCbMCreatorUserId { get; set; }

        public DateTime? NlpCbMCreationTime { get; set; }

        public virtual bool Rebuild { get; set; }
    }
}