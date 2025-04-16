using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpFacebookUsers")]
    public class NlpFacebookUser : Entity<Guid>
    {

        [StringLength(NlpFacebookUserConsts.MaxUserIdLength, MinimumLength = NlpFacebookUserConsts.MinUserIdLength)]
        public virtual string UserId { get; set; }

        [StringLength(NlpFacebookUserConsts.MaxUserNameLength, MinimumLength = NlpFacebookUserConsts.MinUserNameLength)]
        public virtual string UserName { get; set; }

        [StringLength(NlpFacebookUserConsts.MaxPictureUrlLength, MinimumLength = NlpFacebookUserConsts.MinPictureUrlLength)]
        public virtual string PictureUrl { get; set; }

    }
}