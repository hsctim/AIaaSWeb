using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpClientInfos")]
    public class NlpClientInfo : Entity<Guid>, IMustHaveTenant
    {
        public int TenantId { get; set; }

        public virtual Guid ClientId { get; set; }

        [StringLength(NlpClientInfoConsts.MaxConnectionProtocolLength, MinimumLength = NlpClientInfoConsts.MinConnectionProtocolLength)]
        public virtual string ConnectionProtocol { get; set; }

        public virtual DateTime UpdatedTime { get; set; }

        [StringLength(NlpClientInfoConsts.MaxIPLength, MinimumLength = NlpClientInfoConsts.MinIPLength)]
        public virtual string IP { get; set; }

        [StringLength(NlpClientInfoConsts.MaxClientChannelLength, MinimumLength = NlpClientInfoConsts.MinClientChannelLength)]
        public virtual string ClientChannel { get; set; }

    }
}