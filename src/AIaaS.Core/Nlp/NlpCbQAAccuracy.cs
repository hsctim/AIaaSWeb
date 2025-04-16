using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpCbQAAccuracies")]
    public class NlpCbQAAccuracy : CreationAuditedEntity<Guid>, IMustHaveTenant
    {
        public int TenantId { get; set; }

        [Required]
        [StringLength(NlpCbQAAccuracyConsts.MaxQuestionLength, MinimumLength = NlpCbQAAccuracyConsts.MinQuestionLength)]
        public virtual string Question { get; set; }

        public virtual double? AnswerAcc1 { get; set; }

        public virtual double? AnswerAcc2 { get; set; }

        public virtual double? AnswerAcc3 { get; set; }

        public virtual Guid NlpChatbotId { get; set; }

        [ForeignKey("NlpChatbotId")]
        public NlpChatbot NlpChatbotFk { get; set; }

        public virtual Guid? AnswerId1 { get; set; }

        [ForeignKey("AnswerId1")]
        public NlpQA AnswerId1Fk { get; set; }

        public virtual Guid? AnswerId2 { get; set; }

        [ForeignKey("AnswerId2")]
        public NlpQA AnswerId2Fk { get; set; }

        public virtual Guid? AnswerId3 { get; set; }

        [ForeignKey("AnswerId3")]
        public NlpQA AnswerId3Fk { get; set; }

    }
}