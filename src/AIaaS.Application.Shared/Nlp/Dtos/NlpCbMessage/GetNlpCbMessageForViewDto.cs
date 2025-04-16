using Newtonsoft.Json;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetNlpCbMessageForViewDto
    {
        //public NlpCbMessageDto NlpCbMessage { get; set; }
        public string NlpMessage { get; set; }

        public DateTime NlpCreationTime { get; set; }


        public string NlpCbSentType { get; set; }
        public string NlpCbSenderRoleName { get; set; }
        public string NlpCbReceiverRoleName { get; set; }

        public string NlpChatbotName { get; set; }

        public string NlpCbAgentName { get; set; }

        public string NlpClientName { get; set; }

        public string ChannelName { get; set; }

        public Guid? ClientId { get; set; }

        public string PriorWfS { get; set; }

        public string CurrentWfS { get; set; }
    }
}