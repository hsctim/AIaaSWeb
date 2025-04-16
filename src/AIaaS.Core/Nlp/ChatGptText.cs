using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("ChatGptTexts")]
    public class ChatGptText : Entity<Guid>
    {
        public virtual DateTime CreationTime { get; set; }

        [StringLength(ChatGptTextConsts.MaxKeyLength, MinimumLength = ChatGptTextConsts.MinKeyLength)]
        public virtual string Key { get; set; }

        [StringLength(ChatGptTextConsts.MaxTextLength, MinimumLength = ChatGptTextConsts.MinTextLength)]
        public virtual string Text { get; set; }

    }
}