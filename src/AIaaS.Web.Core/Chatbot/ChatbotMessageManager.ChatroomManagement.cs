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


        public async Task<NlpChatroomStatus> GetChatroomStatus(Guid chatbotId, Guid clientId)
        {
            NlpChatroomStatus data = null;
            bool bNeedUpdate = false;

            if (__nlpChatroomStatusCache != null && __nlpChatroomStatusCache.ChatbotId == chatbotId && __nlpChatroomStatusCache.ClientId == clientId)
                data = __nlpChatroomStatusCache;

            data ??= (NlpChatroomStatus)_cacheManager.Get_ChatroomStatus(chatbotId, clientId);

            if (data == null)
            {
                bNeedUpdate = true;
                const int days = 30;

                var message = await _nlpCbMessageRepository.GetAll().Where(e => e.NlpChatbotId == chatbotId && e.ClientId == clientId && e.NlpCreationTime > Clock.Now.AddDays(-days)).OrderByDescending(t => t.NlpCreationTime).FirstOrDefaultAsync();

                var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
                if (chatbot.Disabled == false && chatbot.IsDeleted == false)
                {
                    data = new NlpChatroomStatus()
                    {
                        ChatbotId = chatbot.Id,
                        ChatbotPictureId = chatbot.ChatbotPictureId,
                        ClientId = clientId,
                        ChatbotName = chatbot.Name,
                        LatestMessageTime = message?.NlpCreationTime ?? Clock.Now,
                        ClientConnected = false,
                        ResponseConfirmEnabled = false,
                        IncorrectAnswerCount = 0,
                    };
                }
            }

            if (data == null)
            {
                bNeedUpdate = true;
                data = new NlpChatroomStatus()
                {
                    ChatbotId = chatbotId,
                    ClientId = clientId,
                    LatestMessageTime = Clock.Now,
                    ClientConnected = false,
                    ResponseConfirmEnabled = false,
                    IncorrectAnswerCount = 0,
                };
            }

            //若Client圖像為NULL,設為預設
            if (data.ClientPicture.IsNullOrEmpty())
            {
                data.ClientPicture = "/Common/Images/default-profile-picture.png";
                bNeedUpdate = true;
            }

            //更新快取內的名字跟照片
            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            if (nlpChatbot.Name != data.ChatbotName && nlpChatbot.ChatbotPictureId != data.ChatbotPictureId)
            {
                data.ChatbotName = nlpChatbot.Name;
                data.ChatbotPictureId = nlpChatbot.ChatbotPictureId;
                bNeedUpdate = true;
            }

            //更新快取內的名字跟照片
            if (data.ChatroomAgents != null && data.ChatroomAgents.Count > 0)
            {
                foreach (var agent in data.ChatroomAgents)
                {
                    var agentNamePicture = await GetAgentNamePicture(agent.AgentId);

                    if (agent.AgentName != agentNamePicture.AgentName || agent.AgentPictureId != agentNamePicture.AgentPictureId)
                    {
                        agent.AgentName = agentNamePicture.AgentName;
                        agent.AgentPictureId = agentNamePicture.AgentPictureId;
                        bNeedUpdate = true;
                    }
                }
            }

            var clientInfo = await GetNlpClientInfoDtosCache(nlpChatbot.TenantId, clientId);
            if (clientInfo != null && data.ConnectionProtocol != clientInfo.ConnectionProtocol && data.ClientIP != clientInfo.IP && data.ClientChannel != clientInfo.ClientChannel)
            {
                data.ConnectionProtocol = clientInfo.ConnectionProtocol;
                data.ClientIP = clientInfo.IP;
                data.ClientChannel = clientInfo.ClientChannel;
                bNeedUpdate = true;
            }

            if (clientInfo != null && (clientInfo.ClientChannel == "line" || clientInfo.ConnectionProtocol == "line"))
            {
                var lineUser = _nlpLineUsersAppService.GetNlpLineUserDto(clientId);
                if (data.ClientName != lineUser.UserName || data.ClientPicture != lineUser.PictureUrl)
                {
                    data.ClientName = lineUser.UserName;
                    data.ClientPicture = lineUser.PictureUrl;
                    bNeedUpdate = true;
                }
            }

            if (clientInfo != null && (clientInfo.ClientChannel == "facebook" || clientInfo.ConnectionProtocol == "facebook"))
            {
                var facebookU = _nlpFacebookUsersAppService.GetNlpFacebookUserDto(clientId);
                if (data.ClientName != facebookU.UserName || data.ClientPicture != facebookU.PictureUrl)
                {
                    data.ClientName = facebookU.UserName;
                    data.ClientPicture = facebookU.PictureUrl;
                    bNeedUpdate = true;
                }
            }


            if (bNeedUpdate == true)
                UpdateChatroomStatusCache(chatbotId, clientId, data);

            return data;
        }


        public NlpChatroomStatus UpdateChatroomStatusCache(Guid chatbotId, Guid clientId, NlpChatroomStatus chatroomStatus)
        {
            __nlpChatroomStatusCache = chatroomStatus;
            return (NlpChatroomStatus)_cacheManager.Set_ChatroomStatus(chatbotId, clientId, chatroomStatus);
        }


        /// <summary>
        /// 將人員加入或退出聊天室
        /// </summary>
        /// <param name="add">加入或退出</param>
        /// <param name="chatroomAgent"></param>
        /// <param name="chatroomStatus">如果是空白，從資料庫取得</param>
        public async Task<NlpChatroomStatus> AddRemoveAgentFromChatroom(bool add, NlpAgentInChatroom chatroomAgent, NlpChatroomStatus chatroomStatus)
        {
            bool bNeedUpdate = false;

            chatroomStatus ??= await GetChatroomStatus(chatroomAgent.Chatroom.ChatbotId, chatroomAgent.Chatroom.ClientId);
            chatroomStatus.ChatroomAgents ??= new List<NlpChatroomAgent>();

            NlpChatbotDto nlpChatbot = null;

            //檢查Client是否已離線
            if (chatroomStatus.ClientConnected == true)
            {
                var clientConnection = (NlpClientConnection)_cacheManager.Get_NlpClientConnection_By_ChatbotId_ClientId(chatroomAgent.Chatroom.ChatbotId, chatroomAgent.Chatroom.ClientId);

                if (clientConnection != null && clientConnection.ConnectionId.IsNullOrEmpty() == false)
                {
                    var online_connection = _onlineClientManager.GetAllClients().Where(e => e.ConnectionId == clientConnection.ConnectionId).Select(e => e.ConnectionId).FirstOrDefault();
                    if (online_connection.IsNullOrEmpty())
                        chatroomStatus.ClientConnected = false;
                }
            }

            if (add == false)
            {
                _cacheManager.Remove_ChatroomByAgent(chatroomAgent.AgentId);

                if (chatroomStatus.ChatroomAgents.RemoveAll(c => c.AgentId == chatroomAgent.AgentId) > 0)
                    bNeedUpdate = true;
            }
            else
            {
                //先移除人員在其它聊天室
                var currentChatroom = (NlpChatroom)_cacheManager.Get_ChatroomByAgent(chatroomAgent.AgentId);
                if (currentChatroom != null && (currentChatroom.ChatbotId != chatroomAgent.Chatroom.ChatbotId || currentChatroom.ClientId != chatroomAgent.Chatroom.ClientId))
                {
                    var newStatus = await AddRemoveAgentFromChatroom(false, new NlpAgentInChatroom() { AgentId = chatroomAgent.AgentId, Chatroom = currentChatroom }, null);
                    nlpChatbot ??= _nlpChatbotFunction.GetChatbotDto(chatroomStatus.ChatbotId);
                    SendChatroomStatusToAgents(nlpChatbot.TenantId, newStatus);
                }

                if (chatroomStatus.ChatroomAgents.Count(c => c.AgentId == chatroomAgent.AgentId) == 0)
                {
                    chatroomStatus.ChatroomAgents.Add(await GetAgentNamePicture(chatroomAgent.AgentId));
                    _cacheManager.Set_ChatroomByAgent(chatroomAgent.AgentId, new NlpChatroom() { ChatbotId = chatroomStatus.ChatbotId, ClientId = chatroomStatus.ClientId });
                    bNeedUpdate = true;
                }
            }

            //刪除未連線的
            foreach (var agentId in chatroomStatus.ChatroomAgents)
            {
                nlpChatbot ??= _nlpChatbotFunction.GetChatbotDto(chatroomAgent.Chatroom.ChatbotId);
                UserIdentifier user = new UserIdentifier(nlpChatbot.TenantId, agentId.AgentId);

                if (_onlineClientManager.GetAllByUserId(user).Count == 0)
                {
                    chatroomStatus.ChatroomAgents.RemoveAll(c => c.AgentId == agentId.AgentId);
                    bNeedUpdate = true;
                }
            }

            if (bNeedUpdate)
            {
                UpdateChatroomStatusCache(chatroomStatus.ChatbotId, chatroomStatus.ClientId, chatroomStatus);
            }
            return chatroomStatus;
        }


        private async Task<NlpChatroomStatus> AddMessageToChatroomStatus(Guid chatbotId, Guid clientId, NlpChatroomMessage nlpChatroomMessage)
        {
            var chatroomStatus = await GetChatroomStatus(chatbotId, clientId);
            //var nlpchatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);

            if (chatroomStatus != null)
            {
                chatroomStatus.LatestMessages ??= new List<NlpChatroomMessage>();
                chatroomStatus.LatestMessages.Add(nlpChatroomMessage);
                chatroomStatus.LatestMessages = chatroomStatus.LatestMessages.TakeLast(2).ToList();
                chatroomStatus.UnreadMessageCount++;
                chatroomStatus.LatestMessageTime = Clock.Now;
                UpdateChatroomStatusCache(chatbotId, clientId, chatroomStatus);
            }

            return chatroomStatus;
        }

        private void SendChatroomStatusToAgents(int tenantId, NlpChatroomStatus chatroomStatus)
        {
            if (chatroomStatus.LatestMessages != null && chatroomStatus.LatestMessages.Count > 0)
                _chatbotCommunicator.SendMessageToTenant(tenantId, "updateChatroomStatus", chatroomStatus.ToDictionary());
        }


    }
}
