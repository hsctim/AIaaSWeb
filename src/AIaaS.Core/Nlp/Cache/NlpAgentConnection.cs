using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    public class NlpAgentConnection
    {
        public virtual int AgentTenantId { get; set; }
        public virtual long AgentId { get; set; }

        public virtual Guid? ChatbotId { get; set; }
        public virtual Guid? ClientId { get; set; }

        public virtual string ConnectionId { get; set; }

        public virtual DateTime UpdatedTime { get; set; }
        public virtual bool Connected { get; set; }
    }
}