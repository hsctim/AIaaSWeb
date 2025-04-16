using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp.Dtos
{
    public class CreateOrEditNlpCbTrainedAnswerDto : EntityDto<Guid?>
    {
        [Required]
        public string NlpCbTAAnswer { get; set; }

        public Guid NlpCbTrainingDataId { get; set; }

        public int NNID { get; set; }
    }
}