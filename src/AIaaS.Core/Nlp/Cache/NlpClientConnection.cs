using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    public class NlpClientConnection
    {
        public virtual Guid ClientId { get; set; }

        public virtual Guid ChatbotId { get; set; }

        public virtual Guid ChatbotPictureId { get; set; }

        public virtual string ConnectionId { get; set; }

        public virtual long? AgentId { get; set; }

        public virtual DateTime UpdatedTime { get; set; }

        public virtual bool Connected { get; set; }

        public virtual string ClientIP { get; set; }
        public virtual string ClientChannel { get; set; }
    }
}