using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Services;
using Abp.Domain.Services;
using Abp.RealTime;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using AIaaS.Web.Chatbot.Dto;
using Microsoft.AspNetCore.SignalR;

namespace AIaaS.Chatbot
{
    public class ChatbotMessageType
    {
        public const string TEXT = "text";
    }

    public interface IChatbotMessageManager : IApplicationService
    {
        Task ReceiveClientSignalRMessage(ChatbotMessageManagerMessageDto input);

        Task<IList<ChatbotMessageManagerMessageDto>> ReceiveClientHttpMessage(ChatbotMessageManagerMessageDto input);

        Task<IList<ChatbotMessageManagerMessageDto>> ReceiveClientLineMessage(ChatbotMessageManagerMessageDto input);
        Task ReceiveClientFacebookMessage(ChatbotMessageManagerMessageDto input);

        Task ReceiveClientReceipt(ChatbotMessageManagerMessageDto input);

        Task<IList<ChatbotMessageManagerMessageDto>> GetMessagesByHttp(ChatbotMessageManagerMessageDto input);

        Task SendClientHistoryMessages(IClientProxy caller, ChatbotMessageManagerMessageDto input);

        Task SendClientGreetingMessage(IClientProxy caller, ChatbotMessageManagerMessageDto input);

        void SendErrorMessage(IClientProxy client, string errorMessage);

        Task ReceiveAgentMessage(ChatbotMessageManagerMessageDto input);

        Task AgentRequestHistoryMessages(ChatbotMessageManagerMessageDto input);

        Task OnAgentSendReceipt(ChatbotMessageManagerMessageDto input);

        Task OnClientSendReceipt(Guid chatbotId, Guid clientId);

        Task AgentEnableResponseConfirm(ChatbotMessageManagerMessageDto input, bool enableResponseConfirm);

        Task ClientReconnect(ChatbotMessageManagerMessageDto input);

        Task AgentReconnect(ChatbotMessageManagerMessageDto input);

        Task DisconnectNotification(string connectionId);

        Task<ChatroomWorkflowInfo> SetChatbotWorkflowState(SetChatroomWorkflow workflow);
        Task<ChatroomWorkflowInfo> GetChatbotWorkflowState(NlpChatroom chatroomId);

        bool IsValidChatroom(Guid chatbotId);

        Task<string> ReplaceCustomStringAsync(string text, Guid chatbotId);

    }
}
