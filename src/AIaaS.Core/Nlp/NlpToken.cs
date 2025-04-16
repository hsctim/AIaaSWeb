using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpTokens")]
    public class NlpToken : FullAuditedEntity<Guid>
    {

        [Required]
        [StringLength(NlpTokenConsts.MaxNlpTokenTypeLength, MinimumLength = NlpTokenConsts.MinNlpTokenTypeLength)]
        public virtual string NlpTokenType { get; set; }

        [Required]
        [StringLength(NlpTokenConsts.MaxNlpTokenValueLength, MinimumLength = NlpTokenConsts.MinNlpTokenValueLength)]
        public virtual string NlpTokenValue { get; set; }

    }
}