using System;
using Abp.Application.Services.Dto;
using Abp.Timing;

namespace AIaaS.Nlp.Dtos
{
    public class NlpClientInfoDto : EntityDto<Guid>
    {
        public NlpClientInfoDto(int tenantId, Guid clientId, string connectionProtocol, string ip, string clientChannel, string clientToken)
        {
            TenantId = tenantId;
            ClientId = clientId;
            ConnectionProtocol = connectionProtocol;
            IP = ip;
            ClientChannel = clientChannel;
            UpdatedTime = Clock.Now;
        }

        public NlpClientInfoDto()
        {
            UpdatedTime = Clock.Now;
        }

        public bool isSame(NlpClientInfoDto data)
        {
            if (data == null || data.TenantId != TenantId || data.ClientChannel != ClientChannel || data.ClientId != ClientId || data.ConnectionProtocol != ConnectionProtocol || data.IP != IP)
                return false;

            return true;
        }

        public int TenantId { get; set; }

        public virtual Guid ClientId { get; set; }

        public virtual string ConnectionProtocol { get; set; }

        public virtual DateTime UpdatedTime { get; set; }

        public virtual string IP { get; set; }

        public virtual string ClientChannel { get; set; }

    }
}