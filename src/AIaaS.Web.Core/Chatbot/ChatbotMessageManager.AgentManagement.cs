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
        /// <summary>
        /// Agent User送訊號至Server
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="input"></param>
        [DisableAuditing]
        public async Task ReceiveAgentMessage(ChatbotMessageManagerMessageDto input)
        {
            if (this.IsGranted(AppPermissions.Pages_NlpChatbot_NlpCbAgentOperations_SendMessage) == false)
                return;

            //_lpCbWebApiClient.PrepareQueryPython();

            AddNlpAgentConnectionCache(new NlpAgentConnection()
            {
                ClientId = input.ClientId.Value,
                ChatbotId = input.ChatbotId.Value,
                ConnectionId = input.ConnectionId,
                UpdatedTime = Clock.Now,
                AgentId = input.AgentId.Value,
                AgentTenantId = input.AgentTenantId.Value,
                Connected = true,
            });

            //AddRemoveAgentFromChatroom(true, new NlpAgentInChatroom(input.AgentId.Value, input.ChatbotId.Value, input.ClientId.Value), null);

            //取得Chatbot回覆
            input.MessageType ??= "text";
            input.SenderTime ??= Clock.Now;

            if (string.IsNullOrEmpty(input.SenderRole))
                input.SenderRole = "agent";

            List<NlpCbMessageEx> nlpCbMessageExList = await ProcessReceiveMessage(input);

            //傳送ChatroomStatus至Agents
            if (input.ReceiverRole == "client")
            {
                var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
                var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);

                _cacheManager.Set_ChatbotController_GetMessages_HasData(chatbot.TenantId, input.ClientId.Value, true);

                if (chatroomStatus != null)
                {
                    if (chatroomStatus.UnreadMessageCount != 0)
                    {
                        chatroomStatus.UnreadMessageCount = 0;
                        UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroomStatus);
                        SendChatroomStatusToAgents(chatbot.TenantId, chatroomStatus);
                    }

                    input.ClientId = chatroomStatus.ClientId;
                }
            }

            //傳送未讀的Message至Agents跟Client
            IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(input.ChatbotId.Value, input.ClientId.Value, 1, true);

            await SendAgesntsClientNonReadMessage(input.ChatbotId.Value, input.ClientId.Value, messages, true);

            if (input.ReceiverRole == "chatbot")
            {
                if (nlpCbMessageExList != null)
                {
                    foreach (var nlpCbMessageEx in nlpCbMessageExList)
                    {
                        var output = input;
                        output.ReceiverRole = "agent";
                        output.SenderRole = "chatbot";
                        output.Message = "";
                        output.SuggestedAnswers = JsonConvert.SerializeObject(nlpCbMessageEx.SuggestedAnswers);

                        var messageList = new List<ChatbotMessageManagerMessageDto>();
                        messageList.Add(output);

                        await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, messageList);
                    }
                }

                if (_deferredSendMessageToChatroomAgent != null)
                    await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, _deferredSendMessageToChatroomAgent);
            }

            //_lpCbWebApiClient.PrepareQueryPython();
        }


        [DisableAuditing]
        public async Task OnAgentSendReceipt(ChatbotMessageManagerMessageDto input)
        {
            if (input.AgentId != AbpSession.UserId || input.AgentTenantId != AbpSession.TenantId || input.ChatbotId == null || input.ClientId == null)
                return;

            AddNlpAgentConnectionCache(new NlpAgentConnection()
            {
                ClientId = input.ClientId.Value,
                ChatbotId = input.ChatbotId.Value,
                ConnectionId = input.ConnectionId,
                UpdatedTime = Clock.Now,
                AgentId = input.AgentId.Value,
                AgentTenantId = input.AgentTenantId.Value,
                Connected = true,
            });

            await AddRemoveAgentFromChatroom(true, new NlpAgentInChatroom(input.AgentId.Value, input.ChatbotId.Value, input.ClientId.Value), null);


            DateTime dt30 = Clock.Now.AddDays(-30);

            await _nlpCbMessageRepository.BatchUpdateAsync(
                e => new NlpCbMessage { AlternativeQuestion = null, AgentReadTime = Clock.Now },
                e => e.NlpChatbotId == input.ChatbotId.Value && e.ClientId == input.ClientId && e.NlpCreationTime > dt30 && e.AgentReadTime == null);

            var chatroom = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
            chatroom.UnreadMessageCount = 0;

            SendChatroomStatusToAgents(input.AgentTenantId.Value, chatroom);
        }


        [DisableAuditing]
        public async Task AgentRequestHistoryMessages(ChatbotMessageManagerMessageDto input)
        {
            if (input.AgentId != AbpSession.UserId || input.AgentTenantId != AbpSession.TenantId || input.ChatbotId == null || input.ClientId == null)
                return;

            AddNlpAgentConnectionCache(new NlpAgentConnection()
            {
                ClientId = input.ClientId.Value,
                ChatbotId = input.ChatbotId.Value,
                ConnectionId = input.ConnectionId,
                UpdatedTime = Clock.Now,
                AgentId = input.AgentId.Value,
                AgentTenantId = input.AgentTenantId.Value,
                Connected = true,
            });

            await AddRemoveAgentFromChatroom(true, new NlpAgentInChatroom(input.AgentId.Value, input.ChatbotId.Value, input.ClientId.Value), null);

            //送至Agents
            var agentMessages = await GetNlpCbMessageFromDatabase(input.ChatbotId.Value, input.ClientId.Value);
            if (agentMessages.Count > 0)
            {
                await SendMessageToChatroomAgents(input.ChatbotId.Value, input.ClientId.Value, "agentGetChatbotMessages", agentMessages);
            }
        }

        private async Task SendMessageToChatroomAgents(Guid chatbotId, Guid clientId, string messageName, IList<ChatbotMessageManagerMessageDto> messages)
        {
            var chatroomStatus = await GetChatroomStatus(chatbotId, clientId);
            if (chatroomStatus.ChatroomAgents == null || chatroomStatus.ChatroomAgents.Count == 0)
                return;

            foreach (var message in messages)
            {
                NlpUserNameImage receiverNameImage = await GetNameImage(message.ReceiverRole, message.ReceiverRole == "client" ? message.ClientId : message.ReceiverRole == "chatbot" ? message.ChatbotId : null, message.AgentId, message.ClientChannel);
                message.ReceiverName = receiverNameImage.Name;
                message.ReceiverImage = receiverNameImage.Image;

                NlpUserNameImage senderNameImage = await GetNameImage(message.SenderRole, message.SenderRole == "client" ? message.ClientId : message.SenderRole == "chatbot" ? message.ChatbotId : null, message.AgentId, message.ClientChannel);
                message.SenderName = senderNameImage.Name;
                message.SenderImage = senderNameImage.Image;
            }

            List<IOnlineClient> onlineClientList = new List<IOnlineClient>();

            foreach (var agent in chatroomStatus.ChatroomAgents)
            {
                var nlpAgentConnection = (NlpAgentConnection)_cacheManager.Get_NlpAgentConnection_By_ChatbotId_ClientId_UserId(chatroomStatus.ChatbotId, chatroomStatus.ClientId, agent.AgentId);

                //移除已斷線的Agents
                if (nlpAgentConnection == null || nlpAgentConnection.ConnectionId.IsNullOrEmpty())
                {
                    await AddRemoveAgentFromChatroom(false, new NlpAgentInChatroom() { AgentId = agent.AgentId, Chatroom = new NlpChatroom() { ChatbotId = chatroomStatus.ChatbotId, ClientId = chatroomStatus.ClientId } }, chatroomStatus);
                    RemoveAgentConnectionFromCache(nlpAgentConnection);
                    continue;
                }

                var onlineClient = _onlineClientManager.GetByConnectionIdOrNull(nlpAgentConnection.ConnectionId);
                if (onlineClient != null)
                    onlineClientList.Add(onlineClient);
                else
                {
                    await AddRemoveAgentFromChatroom(false, new NlpAgentInChatroom() { AgentId = agent.AgentId, Chatroom = new NlpChatroom() { ChatbotId = chatroomStatus.ChatbotId, ClientId = chatroomStatus.ClientId } }, chatroomStatus);
                    RemoveAgentConnectionFromCache(nlpAgentConnection);
                }
            }

            if (onlineClientList.Count > 0)
                _chatbotCommunicator.SendMessageToClients(onlineClientList, messageName, ChatbotMessageManagerMessageDto.ToDictionary(messages));
        }

        private void AddNlpAgentConnectionCache(NlpAgentConnection agent)
        {
            _cacheManager.Set_NlpAgentConnection_By_ChatbotId_ClientId_UserId(agent.ChatbotId.Value, agent.ClientId.Value, agent.AgentId, agent);
            _cacheManager.Set_NlpAgentConnection_By_ConnectionId(agent.ConnectionId, agent);
        }

        private void RemoveAgentConnectionFromCache(NlpAgentConnection nlpAgentConnection)
        {
            if (nlpAgentConnection != null)
            {
                _cacheManager.Remove_NlpAgentConnection_By_ChatbotId_ClientId_UserId(nlpAgentConnection.ChatbotId.Value, nlpAgentConnection.ClientId.Value, nlpAgentConnection.AgentId);

                _cacheManager.Remove_NlpAgentConnection_By_ConnectionId(nlpAgentConnection.ConnectionId);
            }
        }
    }
}
