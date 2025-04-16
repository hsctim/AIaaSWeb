using Abp.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AIaaS.Chatbot
{
    public class ChatbotMessageDetails
    {
        public double Acc { get; set; }
        public IList<String> Messages { get; set; }
    }


    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ChatbotMessageManagerMessageDto
    {
        public Guid? Id { get; set; }
        public Guid? ChatbotId { get; set; }
        public Guid? ClientId { get; set; }
        public String ClientToken { get; set; }
        public int? AgentTenantId { get; set; }
        public long? AgentId { get; set; }
        //public int? UserTenantId { get; set; }
        //public long? UserId { get; set; }
        public string ConnectionId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; }
        public string SenderRole { get; set; }
        public string SenderName { get; set; }
        public string SenderImage { get; set; }
        public DateTime? SenderTime { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverImage { get; set; }
        public string ReceiverRole { get; set; }
        public DateTime? AgentReadTime { get; set; }
        public DateTime? ClientReadTime { get; set; }

        // 給Client建議問
        public string AlternativeQuestion { get; set; }
        // 給Agent參考要回覆給Client的答案
        public string SuggestedAnswers { get; set; }
        public string ClientIP { get; set; }
        public string ClientChannel { get; set; }

        public IList<ChatbotMessageDetails> MessageDetails { get; set; }
        //public Guid? QAAccuracyId { get; set; }
        /// <summary>
        /// signal-r, http, websocket, webhook
        /// </summary>
        public string ConnectionProtocol { get; set; }

        public string ErrorMessage { get; set; }

        public string Workflow { get; set; }
        public string WorkflowState { get; set; }

        public int FailedCount { get; set; }

        public int WaitingTimeOut { get; set; }    //GetMessage 設定Timeout時間


        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> d = new Dictionary<string, object>(10);

            if (Id != null) d["id"] = Id;
            if (ChatbotId != null) d["chatbotId"] = ChatbotId;
            if (ClientId != null) d["clientId"] = ClientId;
            if (AgentTenantId != null) d["agentTenantId"] = AgentTenantId;
            if (AgentId != null) d["agentId"] = AgentId;
            if (ConnectionId.IsNullOrEmpty() == false) d["connectionId"] = ConnectionId;
            if (Message.IsNullOrEmpty() == false) d["message"] = Message;
            if (MessageType.IsNullOrEmpty() == false) d["messageType"] = MessageType;

            if (SenderRole.IsNullOrEmpty() == false) d["senderRole"] = SenderRole;
            if (SenderName.IsNullOrEmpty() == false) d["senderName"] = SenderName;
            if (SenderImage.IsNullOrEmpty() == false) d["senderImage"] = SenderImage;
            if (ReceiverName.IsNullOrEmpty() == false) d["receiverName"] = ReceiverName;
            if (ReceiverImage.IsNullOrEmpty() == false) d["receiverImage"] = ReceiverImage;
            if (ReceiverRole.IsNullOrEmpty() == false) d["receiverRole"] = ReceiverRole;

            if (SenderTime != null) d["senderTime"] = SenderTime;
            if (AgentReadTime != null) d["agentReadTime"] = AgentReadTime;
            if (ClientReadTime != null) d["clientReadTime"] = ClientReadTime;

            if (AlternativeQuestion.IsNullOrEmpty() == false) d["alternativeQuestion"] = AlternativeQuestion;
            if (SuggestedAnswers.IsNullOrEmpty() == false) d["suggestedAnswers"] = SuggestedAnswers;

            if (ClientIP.IsNullOrEmpty() == false) d["clientIP"] = ClientIP;
            if (ClientChannel.IsNullOrEmpty() == false) d["clientChannel"] = ClientChannel;

            if (MessageDetails != null) d["messageDetails"] = MessageDetails;

            if (ConnectionProtocol.IsNullOrEmpty() == false) d["connectionProtocol"] = ConnectionProtocol;

            if (ErrorMessage.IsNullOrEmpty() == false) d["errorMessage"] = ErrorMessage;

            if (Workflow.IsNullOrEmpty() == false) d["workflow"] = Workflow;
            if (WorkflowState.IsNullOrEmpty() == false) d["workflowStatus"] = WorkflowState;

            d["failedCount"] = FailedCount;

            return d;
        }

        public static IList<Dictionary<string, object>> ToDictionary(IList<ChatbotMessageManagerMessageDto> source)
        {
            var newList = new List<Dictionary<string, object>>(source.Count);
            foreach (var messageDto in source)
                newList.Add(messageDto.ToDictionary());

            return newList;
        }
    }
}