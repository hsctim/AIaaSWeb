using AIaaS.Nlp;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpCbTrainingDatas")]
    public class NlpCbTrainingData : Entity<Guid>, IMustHaveTenant
    {
        public int TenantId { get; set; }

        public virtual string NlpCbTDSource { get; set; }

        public virtual string NlpNNIDRepetition { get; set; }

        public virtual Guid NlpChatbotId { get; set; }

        [ForeignKey("NlpChatbotId")]
        public NlpChatbot NlpChatbotFk { get; set; }

    }
}