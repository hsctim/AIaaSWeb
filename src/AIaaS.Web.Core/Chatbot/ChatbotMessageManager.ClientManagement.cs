using Abp;
using Abp.Application.Services;
using Abp.Auditing;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.EFPlus;
using Abp.Extensions;
using Abp.RealTime;
using Abp.Runtime.Caching;
using Abp.Timing;
using Abp.UI;
//using static AIaaS.Nlp.NlpCbDictionariesFunction;
using AIaaS.Authorization;
using AIaaS.Chatbot;
using AIaaS.Helper;
using AIaaS.Helpers;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using AIaaS.Nlp.Dtos.NlpCbMessage;
using AIaaS.Nlp.External;
using AIaaS.Nlp.Lib;
using AIaaS.Nlp.Lib.Dtos;
using AIaaS.Sessions;
using AIaaS.Sessions.Dto;
using AIaaS.Web.Chatbot;
using AIaaS.Web.Chatbot.Dto;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ReflectSoftware.Facebook.Messenger.Client;
using ReflectSoftware.Facebook.Messenger.Common.Models;
using ReflectSoftware.Facebook.Messenger.Common.Models.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AIaaS.Web.Chatbot
{
    public partial class ChatbotMessageManager : ApplicationService, IChatbotMessageManager
    {
        [DisableAuditing]
        public async Task SendClientHistoryMessages(IClientProxy caller, ChatbotMessageManagerMessageDto input)
        {
            await AddNlpClientConnectionCache(new NlpClientConnection()
            {
                ClientId = input.ClientId.Value,
                ChatbotId = input.ChatbotId.Value,
                ConnectionId = input.ConnectionId,
                UpdatedTime = Clock.Now,
                AgentId = input.AgentId,
                Connected = true,
                ClientIP = input.ClientIP,
                ClientChannel = input.ClientChannel
            });

            var nlpCbMessages = (await GetNlpCbMessageFromDatabase(input.ChatbotId.Value, input.ClientId.Value))
                .Where(e => e.SenderRole == "client" || e.ReceiverRole == "client").ToList();

            if (nlpCbMessages != null && nlpCbMessages.Count > 0)
            {
                _chatbotCommunicator.SendMessageToClient(caller, "receiveMessages",
                    ChatbotMessageManagerMessageDto.ToDictionary(nlpCbMessages));
            }
        }

        [DisableAuditing]
        public async Task SendClientGreetingMessage(IClientProxy caller, ChatbotMessageManagerMessageDto input)
        {
            await AddNlpClientConnectionCache(new NlpClientConnection()
            {
                ClientId = input.ClientId.Value,
                ChatbotId = input.ChatbotId.Value,
                ConnectionId = input.ConnectionId,
                UpdatedTime = Clock.Now,
                AgentId = input.AgentId,
                Connected = true,
                ClientIP = input.ClientIP,
                ClientChannel = input.ClientChannel
            });

            ChatbotMessageManagerMessageDto messageDto = null;
            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);

            if (nlpChatbot == null || nlpChatbot.Disabled || nlpChatbot.GreetingMsg.IsNullOrEmpty())
                return;

            messageDto = new ChatbotMessageManagerMessageDto()
            {
                Id = Guid.NewGuid(),
                ClientId = input.ClientId,
                ChatbotId = input.ChatbotId,
                Message = await ReplaceCustomStringAsync(nlpChatbot.GreetingMsg.Replace("${Chatbot.Name}", nlpChatbot.Name), input.ChatbotId.Value),
                MessageType = ChatbotMessageType.TEXT,
                ReceiverRole = input.ReceiverRole,
                SenderImage = "/Chatbot/ProfilePicture/" + input.ChatbotId.ToString(),
                SenderRole = "chatbot",
                SenderTime = Clock.Now,
                AgentId = input.AgentId,
            };

            var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
            SendChatroomStatusToAgents(nlpChatbot.TenantId, chatroomStatus);

            var messages = new List<ChatbotMessageManagerMessageDto>(1);
            messages.Add(messageDto);
            _chatbotCommunicator.SendMessageToClient(caller, "receiveMessages", ChatbotMessageManagerMessageDto.ToDictionary(messages));
        }

        public async Task ClientReconnect(ChatbotMessageManagerMessageDto input)
        {
            await AddNlpClientConnectionCache(new NlpClientConnection()
            {
                ClientId = input.ClientId.Value,
                ChatbotId = input.ChatbotId.Value,
                ConnectionId = input.ConnectionId,
                UpdatedTime = Clock.Now,
                AgentId = input.AgentId,
                Connected = true,
                ClientIP = input.ClientIP,
                ClientChannel = input.ClientChannel
            });

            //傳送ChatroomStatus至Agents
            var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
            var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);

            if (chatroomStatus != null)
            {
                if (input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol)
                {
                    chatroomStatus.ConnectionProtocol = input.ConnectionProtocol;
                    UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroomStatus);
                }
                SendChatroomStatusToAgents(chatbot.TenantId, chatroomStatus);
            }

            //傳送未讀的Message至Agents跟Client
            IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true);

            await SendAgesntsClientNonReadMessage(chatbot.Id, input.ClientId.Value, messages, true);
        }

        private async Task AddNlpClientConnectionCache(NlpClientConnection client)
        {
            _cacheManager.Set_NlpClientConnection_By_ChatbotId_ClientId(client.ChatbotId, client.ClientId, client);
            _cacheManager.Set_NlpClientConnection_By_ConnectionId(client.ConnectionId, client);

            var chatroomStatus = await GetChatroomStatus(client.ChatbotId, client.ClientId);
            if (chatroomStatus != null)
            {
                if (chatroomStatus.ClientConnected != true || chatroomStatus.ClientChannel != client.ClientChannel || chatroomStatus.ClientIP != client.ClientIP)
                {
                    chatroomStatus.ClientConnected = true;
                    chatroomStatus.ClientChannel = client.ClientChannel;
                    chatroomStatus.ClientIP = client.ClientIP;
                    UpdateChatroomStatusCache(client.ChatbotId, client.ClientId, chatroomStatus);
                }
            }
        }

        private async Task RemoveClientConnectionFromCache(NlpClientConnection nlpClientConnection)
        {
            if (nlpClientConnection != null)
            {
                _cacheManager.Remove_NlpClientConnection_By_ChatbotId_ClientId(nlpClientConnection.ChatbotId, nlpClientConnection.ClientId);

                _cacheManager.Remove_NlpClientConnection_By_ConnectionId(nlpClientConnection.ConnectionId);

                var chatroomStatus = await GetChatroomStatus(nlpClientConnection.ChatbotId, nlpClientConnection.ClientId);

                if (chatroomStatus != null && chatroomStatus.ClientConnected != false)
                {
                    chatroomStatus.ClientConnected = false;
                    UpdateChatroomStatusCache(nlpClientConnection.ChatbotId, nlpClientConnection.ClientId, chatroomStatus);
                }
            }
        }

    }
}
