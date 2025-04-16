using System;

namespace AIaaS.Web.Chatbot.SignalR
{
    public class SendChatbotMessageInput
    {
        public Guid? ChatbotId { get; set; }
        public long? AgentId { get; set; }
        public int? AgentTenantId { get; set; }
        //public long? UserId { get; set; }
        //public int? UserTenantId { get; set; }
        public Guid? ClientId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; }
        public string SenderRole { get; set; }
        public string SenderName { get; set; }
        public string SenderImage { get; set; }
        public string ReceiverRole { get; set; }
        public DateTime? SenderTime { get; set; }
        public bool? EnableResponseConfirm { get; set; }
        //public DateTime? ReceiverTime { get; set; }
    }
}