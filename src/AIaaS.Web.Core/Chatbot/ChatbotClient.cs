using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIaaS.Web.Chatbot
{
    public class ChatbotClient
    {
        public IClientProxy caller { get; set; }
        public int? tenantId { get; set; }
        public long? userId { get; set; }
        public Guid? ClientId { get; set; }
    }
}
