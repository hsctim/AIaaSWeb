using AIaaS.Nlp;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpCbTrainedAnswers")]
    public class NlpCbTrainedAnswer : Entity<Guid>, IMustHaveTenant
    {
        public int TenantId { get; set; }

        [Required]
        public virtual string NlpCbTAAnswer { get; set; }

        public virtual DateTime CreationTime { get; set; }

        public virtual int NNID { get; set; }

        public virtual Guid NlpCbTrainingDataId { get; set; }

        [ForeignKey("NlpCbTrainingDataId")]
        public NlpCbTrainingData NlpCbTrainingDataFk { get; set; }

    }
}