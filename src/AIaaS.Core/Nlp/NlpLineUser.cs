using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpLineUsers")]
    public class NlpLineUser : Entity<Guid>
    {

        [Required]
        [StringLength(NlpLineUserConsts.MaxUserIdLength, MinimumLength = NlpLineUserConsts.MinUserIdLength)]
        public virtual string UserId { get; set; }

        [StringLength(NlpLineUserConsts.MaxUserNameLength, MinimumLength = NlpLineUserConsts.MinUserNameLength)]
        public virtual string UserName { get; set; }

        [StringLength(NlpLineUserConsts.MaxPictureUrlLength, MinimumLength = NlpLineUserConsts.MinPictureUrlLength)]
        public virtual string PictureUrl { get; set; }

    }
}