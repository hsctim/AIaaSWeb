using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using Abp.Application.Services.Dto;
using AIaaS.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.Runtime.Caching;
using Abp.Runtime.Session;
using AIaaS.Helpers;
using Abp.Application.Services;
using ApiProtectorDotNet;
using Abp.Timing;

namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbAgentOperations)]
    public class NlpCbAgentOperationsAppService : AIaaSAppServiceBase, INlpCbAgentOperationsAppService
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRepository<NlpCbMessage, Guid> _nlpCbMessageRepository;
        private readonly IRepository<NlpChatbot, Guid> _nlpChatbotRepository;
        private readonly IRepository<NlpClientInfo, Guid> _nlpClientInfo;
        private readonly IRepository<NlpLineUser, Guid> _nlpLineUserRepository;
        private readonly NlpCbSession _nlpCbSession;
        private readonly NlpChatbotFunction _nlpChatbotFunction;

        public NlpCbAgentOperationsAppService(
            IRepository<NlpCbMessage, Guid> nlpCbMessageRepository,
            IRepository<NlpChatbot, Guid> nlpChatbotRepository,
            IRepository<NlpClientInfo, Guid> nlpClientInfo,
            IRepository<NlpLineUser, Guid> nlpLineUserRepository,
            NlpChatbotFunction nlpChatbotFunction,
            ICacheManager cacheManager,
            NlpCbSession nlpCbSession
            )
        {
            _nlpCbMessageRepository = nlpCbMessageRepository;
            _nlpChatbotRepository = nlpChatbotRepository;
            _nlpClientInfo = nlpClientInfo;
            _nlpLineUserRepository = nlpLineUserRepository;
            _nlpChatbotFunction = nlpChatbotFunction;
            _cacheManager = cacheManager;
            _nlpCbSession = nlpCbSession;
        }




        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<List<NlpChatroomStatus>> GetAllChatrooms(Guid? chatbotId)
        {
            const int days = 30;

            int? tenantId = AbpSession.TenantId;
            if (tenantId == null)
                return null;

            chatbotId ??= Guid.Parse(NlpCbAgentOperationsConst.TenantScope);
            _nlpCbSession["ChatbotId"] = chatbotId?.ToString();

            if (chatbotId.HasValue && chatbotId.Value.ToString() == NlpCbAgentOperationsConst.TenantScope)
                chatbotId = null;

            var messageFilter = _nlpCbMessageRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId.Value && e.NlpCreationTime > Clock.Now.AddDays(-days))
                .WhereIf(chatbotId.HasValue, e => e.NlpChatbotId == chatbotId.Value);

            var chatbotFilter = _nlpChatbotRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId.Value && e.Disabled == false)
                .WhereIf(chatbotId.HasValue, e => e.Id == chatbotId.Value);

            var clientInfoFilter = _nlpClientInfo.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId.Value);

            var lineineUserFilter = _nlpLineUserRepository.GetAll();

            var filteredChatroom = from m in messageFilter
                                   join c in chatbotFilter on m.NlpChatbotId equals c.Id
                                   group m by new { m.ClientId, m.NlpChatbotId } into g
                                   select new NlpChatroomStatus()
                                   {
                                       ChatbotId = g.Key.NlpChatbotId.Value,
                                       ClientId = g.Key.ClientId.Value,
                                       LatestMessageTime = g.Max(t => t.NlpCreationTime),
                                   };

            var filterChatroom2 = from o in filteredChatroom
                                  join c2 in clientInfoFilter on o.ClientId equals c2.ClientId into j2
                                  from s2 in j2.DefaultIfEmpty()
                                  join c3 in lineineUserFilter on o.ClientId equals c3.Id into j3
                                  from s3 in j3.DefaultIfEmpty()
                                  select new NlpChatroomStatus()
                                  {
                                      ChatbotId = o.ChatbotId,
                                      ClientId = o.ClientId,
                                      LatestMessageTime = o.LatestMessageTime,
                                      ClientChannel = s2.ClientChannel,
                                      ClientIP = s2.IP,
                                      ConnectionProtocol = s2.ConnectionProtocol,
                                      ClientName = s3 == null ? null : s3.UserName,
                                      ClientPicture = s3 == null ? null : s3.PictureUrl
                                      //(s3 != null) ? s3.PictureUrl : null
                                  };


            var chatroomList = await filterChatroom2.OrderByDescending(e => e.LatestMessageTime).Take(100).ToListAsync();
            var chatroomList2 = new List<NlpChatroomStatus>();

            NlpChatbotDto chatbotDto = null;

            foreach (var chatroom in chatroomList)
            {
                var chatroom2 = GetChatroomStatus(chatroom.ChatbotId, chatroom.ClientId);
                chatroom2.LatestMessageTime = chatroom.LatestMessageTime;

                if (chatroom.ClientPicture.IsNullOrEmpty() == false)
                    chatroom2.ClientPicture ??= chatroom.ClientPicture;

                if (chatroom2.ClientPicture.IsNullOrEmpty())
                    chatroom2.ClientPicture = "/Common/Images/default-profile-picture.png";

                if (chatroom.ClientName.IsNullOrEmpty() == false)
                    chatroom2.ClientName ??= chatroom.ClientName;

                if (chatbotDto == null || chatbotDto.Id != chatroom.ChatbotId)
                    chatbotDto = _nlpChatbotFunction.GetChatbotDto(chatroom.ChatbotId);

                chatroom2.ChatbotPictureId = chatbotDto.ChatbotPictureId;
                chatroom2.ChatbotName = chatbotDto.Name;

                if (chatroom.ConnectionProtocol.IsNullOrEmpty() == false)
                    chatroom2.ConnectionProtocol = chatroom.ConnectionProtocol;

                if (chatroom.ClientChannel.IsNullOrEmpty() == false)
                    chatroom2.ClientChannel = chatroom.ClientChannel;

                _cacheManager.Set_ChatroomStatus(chatroom2.ChatbotId, chatroom2.ClientId, chatroom2);

                chatroomList2.Add(chatroom2);
            }

            var tenantChatrooms = chatroomList.Select(e => new NlpChatroom()
            {
                ChatbotId = e.ChatbotId,
                ClientId = e.ClientId
            });

            return chatroomList2;
        }


        /// <summary>
        /// 取得最新的狀態，未讀的訊息數，最新兩筆的訊息，Channel，不正確的應答數
        /// </summary>
        /// <param name="nlpChatroom"></param>
        /// <returns></returns>
        [RemoteService(false)]
        public NlpChatroomStatus GetChatroomStatus(Guid chatbotId, Guid clientId)
        {
            const int days = 30;

            NlpChatroomStatus data = (NlpChatroomStatus)_cacheManager.Get_ChatroomStatus(chatbotId, clientId);

            if (data != null && data.LatestMessages != null && data.LatestMessages.Count > 0)
                return data;

            data = new NlpChatroomStatus()
            {
                ChatbotId = chatbotId,
                ClientId = clientId,
                ResponseConfirmEnabled = false
            };

            //如果快取沒資料，查取資料庫，未讀取的資料筆數
            data.UnreadMessageCount = _nlpCbMessageRepository.Count(
               e => e.NlpChatbotId == data.ChatbotId && e.ClientId == data.ClientId && e.AgentReadTime == null && e.NlpCreationTime > Clock.Now.AddDays(-days));

            //取最後兩筆Messages
            var latestData = _nlpCbMessageRepository.GetAll()
                .Where(e => e.NlpChatbotId == data.ChatbotId && e.ClientId == data.ClientId && e.NlpCreationTime >= Clock.Now.AddDays(-30))
                .OrderByDescending(e => e.NlpCreationTime).Take(2).OrderBy(e => e.NlpCreationTime);

            data.LatestMessages = (from o in latestData
                                   select new NlpChatroomMessage()
                                   {
                                       IsClientSent = o.NlpSenderRole == "client",
                                       Message = o.NlpMessage
                                   }).ToList();


            //是否連線
            var clientConnection = (NlpClientConnection)_cacheManager.Get_NlpClientConnection_By_ChatbotId_ClientId(data.ChatbotId, data.ClientId);

            if (clientConnection != null)
            {
                data.ClientConnected = clientConnection.Connected;
                data.ClientIP = clientConnection.ClientIP;
                data.ClientChannel = clientConnection.ClientChannel;
            }

            //data.MessageChannel = "web";
            //data.IncorrectAnswerCount = 2;

            return data;
        }
    }
}
