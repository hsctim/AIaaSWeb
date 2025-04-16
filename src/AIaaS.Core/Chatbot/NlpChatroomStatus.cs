using Abp.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AIaaS.Nlp.Dtos
{
    public class NlpChatroomMessage
    {
        public bool IsClientSent { get; set; }
        public string Message { get; set; }
    }


    public class NlpChatroom
    {
        public Guid ChatbotId { get; set; }
        public Guid ClientId { get; set; }

        public NlpChatroom()
        {
        }

        public NlpChatroom(Guid ChatbotId, Guid ClientId)
        {
            this.ChatbotId = ChatbotId;
            this.ClientId = ClientId;
        }
    }


    public class NlpChatroomAgent
    {
        public long AgentId { get; set; }
        public string AgentName { get; set; }
        public Guid? AgentPictureId { get; set; }
    }

    public class NlpAgentInChatroom
    {
        public long AgentId { get; set; }
        public NlpChatroom Chatroom { get; set; }

        public NlpAgentInChatroom()
        {
        }

        public NlpAgentInChatroom(long AgentId, Guid ChatbotId, Guid ClientId)
        {
            this.AgentId = AgentId;
            this.Chatroom = new NlpChatroom()
            {
                ChatbotId = ChatbotId,
                ClientId = ClientId,
            };
        }
    }

    public class NlpUserNameImage
    {
        public string Name { get; set; }
        public string Image { get; set; }
    }


    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class NlpChatroomStatus
    {
        public NlpChatroomStatus()
        {
            ResponseConfirmEnabled = false;
            //ClientSentReceipt = false;
        }
        public Guid ChatbotId { get; set; }
        public Guid ClientId { get; set; }
        public Guid? ChatbotPictureId { get; set; }
        public string ChatbotName { get; set; }
        public DateTime LatestMessageTime { get; set; }
        public List<NlpChatroomMessage> LatestMessages { get; set; }
        public int UnreadMessageCount { get; set; }
        public int IncorrectAnswerCount { get; set; }

        public List<NlpChatroomAgent> ChatroomAgents { get; set; }

        //public string MessageChannel { get; set; }
        public string ClientIP { get; set; }
        public string ClientName { get; set; }
        public string ClientChannel { get; set; }
        public string ClientPicture { get; set; }
        /// <summary>
        /// 是否連線中
        /// </summary>
        public bool ClientConnected { get; set; }

        /// <summary>
        /// Chatbot會先將答案回應至Agent端，由Agent確認後送回
        /// </summary>
        public bool ResponseConfirmEnabled { get; set; }
        //public bool ClientSentReceipt { get; set; }

        public string ConnectionProtocol { get; set; }

        private Guid _WfState;
        private DateTime _WfStateUpdateTime;
        public Guid WfState
        {
            get
            {
                return (_WfStateUpdateTime.AddMinutes(3) < DateTime.UtcNow) ? Guid.Empty : _WfState;
            }
            set
            {
                _WfState = value;
                _WfStateUpdateTime = DateTime.UtcNow;
            }
        }

        //public int PredictionErrorCount { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>
            {
                { "chatbotId" , ChatbotId },
                { "clientId",  ClientId },

                { "latestMessageTime", LatestMessageTime },
                { "latestMessages", LatestMessages },
                { "unreadMessageCount", UnreadMessageCount},
                //{ "incorrectAnswerCount", IncorrectAnswerCount},
                { "chatroomAgents", ChatroomAgents},

                { "clientConnected", ClientConnected },
                { "responseConfirmEnabled", ResponseConfirmEnabled },

                { "wfState", WfState },
                { "predictionErrorCount", IncorrectAnswerCount },
            };

            if (ChatbotPictureId != null)
                dic["chatbotPictureId"] = ChatbotPictureId;
            if (ChatbotName.IsNullOrEmpty() == false)
                dic["chatbotName"] = ChatbotName;

            if (ClientIP.IsNullOrEmpty() == false)
                dic["clientIP"] = ClientIP;
            if (ClientName.IsNullOrEmpty() == false)
                dic["clientName"] = ClientName;
            if (ClientChannel.IsNullOrEmpty() == false)
                dic["clientChannel"] = ClientChannel;
            if (ClientPicture.IsNullOrEmpty() == false)
                dic["clientPicture"] = ClientPicture;

            if (ConnectionProtocol.IsNullOrEmpty() == false)
                dic["connectionProtocol"] = ConnectionProtocol;

            return dic;
        }
    }

    //public class NlpChatroomStatusDto
    //{
    //    //public Guid ChatbotId;
    //    //public Guid ClientId;
    //    //public Guid? ChatbotPictureId;
    //    public string ChatbotName { get; set; }
    //    //public DateTime LatestMessageTime;
    //    //public List<NlpChatroomMessage> LatestMessages;
    //    //public int UnreadMessageCount { get; set; }
    //    //public int IncorrectAnswerCount { get; set; }

    //    //public List<NlpChatroomAgent> ChatroomAgents;

    //    //public string MessageChannel { get; set; }
    //    //public string ClientIP { get; set; }
    //    //public string ClientName { get; set; }
    //    //public string ClientChannel { get; set; }
    //    //public string ClientPicture { get; set; }
    //    /// <summary>
    //    /// 是否連線中
    //    /// </summary>
    //    //public bool ClientConnected { get; set; }
    //}
}