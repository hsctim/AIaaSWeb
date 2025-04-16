using AIaaS.Nlp;
using AIaaS.Authorization.Users;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpCbModels")]
    public class NlpCbModel : Entity<Guid>, IMustHaveTenant
    {
        public int TenantId { get; set; }

        [Required]
        [StringLength(NlpCbModelConsts.MaxNlpCbMLanguageLength, MinimumLength = NlpCbModelConsts.MinNlpCbMLanguageLength)]
        public virtual string NlpCbMLanguage { get; set; }

        public virtual int NlpCbMStatus { get; set; }

        public virtual DateTime? NlpCbMTrainingStartTime { get; set; }

        public virtual DateTime? NlpCbMTrainingCompleteTime { get; set; }

        public virtual DateTime? NlpCbMTrainingCancellationTime { get; set; }

        [StringLength(NlpCbModelConsts.MaxNlpCbMInfoLength, MinimumLength = NlpCbModelConsts.MinNlpCbMInfoLength)]
        public virtual string NlpCbMInfo { get; set; }

        public virtual long? NlpCbMCreatorUserId { get; set; }

        public virtual DateTime? NlpCbMCreationTime { get; set; }

        public virtual double? NlpCbAccuracy { get; set; }

        public virtual bool Rebuild { get; set; }

        public virtual Guid NlpChatbotId { get; set; }

        [ForeignKey("NlpChatbotId")]
        public NlpChatbot NlpChatbotFk { get; set; }

        public virtual long? NlpCbMTrainingCancellationUserId { get; set; }

        [ForeignKey("NlpCbMTrainingCancellationUserId")]
        public User NlpCbMTrainingCancellationUserFk { get; set; }

        [ForeignKey("NlpCbMCreatorUserId")]
        public User NlpCbMCreatorUserFk { get; set; }

    }
}