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



//using static AIaaS.Nlp.Lib.Dtos.NlpCbGetChatbotPredictResult;

namespace AIaaS.Web.Chatbot
{

    [RemoteService(false)]
    [DisableAuditing]
    public partial class ChatbotMessageManager : ApplicationService, IChatbotMessageManager
    {
        private const int _SemaphoreSlimWaitTimeOut = 60000;

        private readonly IRepository<NlpCbMessage, Guid> _nlpCbMessageRepository;
        private readonly IRepository<NlpCbTrainingData, Guid> _nlpCbTrainingDataRepository;
        private readonly IRepository<NlpCbQAAccuracy, Guid> _nlpCbQAAccuracyRepository;
        private readonly IRepository<NlpQA, Guid> _nlpQARepository;
        private readonly IRepository<NlpWorkflowState, Guid> _nlpWorkflowStateRepository;
        private readonly IRepository<NlpClientInfo, Guid> _nlpClientInfo;
        private readonly IChatbotCommunicator _chatbotCommunicator;
        private readonly IOnlineClientManager<ChatbotChannel> _onlineClientManager;
        private readonly NlpCbWebApiClient _lpCbWebApiClient;
        private readonly OpenAIClient _openAIClient;
        private readonly ICacheManager _cacheManager;
        private readonly ExternalCustomData _externalCustomData;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly ISessionAppService _sessionAppService;
        private readonly INlpLineUsersAppService _nlpLineUsersAppService;
        private readonly INlpFacebookUsersAppService _nlpFacebookUsersAppService;
        private ClientMessenger _clientMessenger;

        private NlpChatroomStatus __nlpChatroomStatusCache;
        private UserLoginInfoDto __userLoginInfoDtoCache;
        private NlpClientInfoDto __nlpClientInfoDtoCache;
        private NlpWorkflowStateInfo __nlpWorkflowStateInfoCache;


        private List<ChatbotMessageManagerMessageDto> _deferredSendMessageToChatroomAgent;

        private readonly INlpPolicyAppService _nlpPolicyAppService;

        //private IHubCallerClients _hubCallerClients;

        public ChatbotMessageManager(
            IRepository<NlpCbMessage, Guid> nlpCbMessageRepository,
            IRepository<NlpCbTrainingData, Guid> nlpCbTrainingDataRepository,
            IRepository<NlpCbQAAccuracy, Guid> nlpCbQAAccuracyRepository,
            IRepository<NlpQA, Guid> nlpQARepository,
            IRepository<NlpWorkflowState, Guid> nlpWorkflowStateRepository,
            IRepository<NlpClientInfo, Guid> nlpClientInfo,
            IChatbotCommunicator chatbotCommunicator,
            IOnlineClientManager<ChatbotChannel> onlineClientManager,
            NlpCbWebApiClient lpCbWebApiClient,
            OpenAIClient openAIClient,
            ICacheManager cacheManager,
            ExternalCustomData externalCustomData,
            NlpChatbotFunction nlpChatbotFunction,
            //NlpCbDictionariesFunction nlpCbDictionariesFunction,
            ISessionAppService sessionAppService,
            INlpLineUsersAppService nlpLineUsersAppService,
            INlpFacebookUsersAppService nlpFacebookUsersAppService,

            INlpPolicyAppService nlpPolicyAppService
            //IHubCallerClients hubCallerClients
            )
        {
            _nlpCbMessageRepository = nlpCbMessageRepository;
            _chatbotCommunicator = chatbotCommunicator;
            _onlineClientManager = onlineClientManager;
            _lpCbWebApiClient = lpCbWebApiClient;
            _openAIClient = openAIClient;
            _nlpCbTrainingDataRepository = nlpCbTrainingDataRepository;
            _cacheManager = cacheManager;
            _externalCustomData = externalCustomData;
            _nlpCbQAAccuracyRepository = nlpCbQAAccuracyRepository;
            _nlpQARepository = nlpQARepository;
            _nlpWorkflowStateRepository = nlpWorkflowStateRepository;
            _nlpClientInfo = nlpClientInfo;
            _nlpChatbotFunction = nlpChatbotFunction;
            //_nlpCbDictionariesFunction = nlpCbDictionariesFunction;
            _sessionAppService = sessionAppService;
            //_hubCallerClients = hubCallerClients;
            _nlpLineUsersAppService = nlpLineUsersAppService;
            _nlpFacebookUsersAppService = nlpFacebookUsersAppService;

            _nlpPolicyAppService = nlpPolicyAppService;
        }


        [DisableAuditing]
        private async Task<NlpCbMessage> GetUnfoundMessageAsync(Guid chatbotId, Guid clientId, string question, List<string> alternativeQuestion, Guid? qaAccuracyId)
        {
            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            var failedMessage = nlpChatbot.FailedMsg;
            if (failedMessage.IsNullOrEmpty() == true)
                return null;

            failedMessage = await ReplaceCustomStringAsync(nlpChatbot.FailedMsg, chatbotId);

            if (string.IsNullOrEmpty(question) == false)
                failedMessage = await ChatGPT(chatbotId, question, failedMessage);

            NlpCbMessage nlpCbMessage = new NlpCbMessage()
            {
                TenantId = nlpChatbot.TenantId,
                ClientId = clientId,
                NlpChatbotId = chatbotId,
                NlpMessage = failedMessage,
                NlpMessageType = "text.error",
                NlpCreationTime = Clock.Now,
                NlpSenderRole = "chatbot",
                NlpReceiverRole = "client",
                NlpAgentId = null,
                AlternativeQuestion = (alternativeQuestion == null) ? null : JsonConvert.SerializeObject(alternativeQuestion),
                QAAccuracyId = qaAccuracyId
            };
            return nlpCbMessage;
        }

        [DisableAuditing]
        private async Task<NlpCbMessage> GetUnfoundMessageWithoutGPTAsync(Guid chatbotId, Guid clientId, string question, List<string> alternativeQuestion, Guid? qaAccuracyId)
        {
            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            var failedMessage = nlpChatbot.FailedMsg;
            if (failedMessage.IsNullOrEmpty() == true)
                return null;

            failedMessage = await ReplaceCustomStringAsync(nlpChatbot.FailedMsg, chatbotId);

            NlpCbMessage nlpCbMessage = new NlpCbMessage()
            {
                TenantId = nlpChatbot.TenantId,
                ClientId = clientId,
                NlpChatbotId = chatbotId,
                NlpMessage = failedMessage,
                NlpMessageType = "text.error",
                NlpCreationTime = Clock.Now,
                NlpSenderRole = "chatbot",
                NlpReceiverRole = "client",
                NlpAgentId = null,
                AlternativeQuestion = (alternativeQuestion == null) ? null : JsonConvert.SerializeObject(alternativeQuestion),
                QAAccuracyId = qaAccuracyId
            };
            return nlpCbMessage;
        }



        protected async Task<NlpCbQAAccuracy> SaveNlpCbQAAccuracy(NlpCbMessage receivedMessage, List<AllPredictMessages> predictResult)
        {
            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(receivedMessage.NlpChatbotId.Value);

            if (nlpChatbot == null || nlpChatbot.Disabled)
                return null;

            using (CurrentUnitOfWork.SetTenantId(nlpChatbot.TenantId))
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MustHaveTenant, AbpDataFilters.MayHaveTenant))
                {
                    NlpCbQAAccuracy nlpAccuracy = new NlpCbQAAccuracy()
                    {
                        CreationTime = Clock.Now,
                        CreatorUserId = null,
                        Question = receivedMessage.NlpMessage,
                        TenantId = nlpChatbot.TenantId,
                        NlpChatbotId = nlpChatbot.Id
                    };

                    int nIndex = 0;
                    foreach (var item in predictResult)
                    {
                        var probability = item.ChatbotPredictResult.probability;

                        //if (nlpChatbot.ModelAccu>=0.1 && nlpChatbot.ModelAccu <= 1)
                        //{
                        //    probability = Math.Min(probability / nlpChatbot.ModelAccu,1);
                        //}

                        switch (nIndex)
                        {
                            case 0:
                                nlpAccuracy.AnswerAcc1 = probability;
                                nlpAccuracy.AnswerId1 = item.ChatbotPredictResult.QaId;
                                break;
                            case 1:
                                nlpAccuracy.AnswerAcc2 = probability;
                                nlpAccuracy.AnswerId2 = item.ChatbotPredictResult.QaId;
                                break;
                            case 2:
                                nlpAccuracy.AnswerAcc3 = probability;
                                nlpAccuracy.AnswerId3 = item.ChatbotPredictResult.QaId;
                                break;
                            default:
                                continue;
                        }
                        nIndex++;
                    }

                    return await _nlpCbQAAccuracyRepository.InsertAsync(nlpAccuracy);
                }
            }
        }

        //檢查NNID1及NNID2是否指向相同的答案
        protected async Task<bool> IsSameNNID(Guid chatbotId, int nnid1, int nnid2)
        {
            try
            {
                Dictionary<int, int[]> nnidDic = await GetAnswerFromNNIDRepetition(chatbotId);

                if (nnidDic != null && nnidDic[nnid1].Contains(nnid2))
                    return true;
            }
            catch (Exception)
            {
            }
            return false;
        }



        protected async Task<string> ChatGPT(Guid chatbotId, string question, string answer)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            if (chatbot == null || chatbot.Disabled || chatbot.EnableOPENAI == (int)NlpChatbotConsts.EnableGPTType.Disabled)
                return answer;

            try
            {
                if (answer.ToUpper().Contains("[GPT]"))
                {
                    var newQuestion = answer
                        .Replace("[GPT]", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("[Question]", question, StringComparison.OrdinalIgnoreCase)
                        .Replace("[Answer]", answer, StringComparison.OrdinalIgnoreCase);

                    var chatResult = await _openAIClient.Chat(chatbotId, newQuestion);
                    if (chatResult != null)
                    {
                        var semaphoreSlim = await _nlpPolicyAppService.GetMessageSendQuotaSemaphoreSlim(chatbot.TenantId);

                        InferenctSlime(semaphoreSlim, chatResult.cost);
                        return chatResult.text;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.ToString(), ex);
            }

            return answer;
        }


        //private bool IsSameWordSegment(int chatbotTenantId, Guid chatbotId, string language, string str1, string str2)
        //{
        //    if (string.Compare(str1, str2, true) == 0)
        //        return true;

        //    var s1 = _nlpCbDictionariesFunction.PrepareSynonymString(chatbotTenantId, chatbotId, language, (Guid.Empty, str1));

        //    var s2 = _nlpCbDictionariesFunction.PrepareSynonymString(chatbotTenantId, chatbotId, language, (Guid.Empty, str2));

        //    return false;
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="days"></param>
        /// <param name="bUnreadOnly">只要未讀的</param>
        /// <returns></returns>
        public async Task<IList<ChatbotMessageManagerMessageDto>> GetNlpCbMessageFromDatabase(Guid chatbotId, Guid clientId, int days = 30, bool bUnreadOnly = false)
        {
            DateTime dt = Clock.Now.AddDays(-days);

            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            if (nlpChatbot == null || nlpChatbot.Disabled)
                return null;

            await CurrentUnitOfWork.SaveChangesAsync();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MustHaveTenant, AbpDataFilters.MayHaveTenant))
            {
                var nlpCbMessagesQuery = await
                    (from o in _nlpCbMessageRepository.GetAll()

                     join o1 in _nlpCbQAAccuracyRepository.GetAll().Include(e => e.AnswerId1Fk).Include(e => e.AnswerId2Fk).Include(e => e.AnswerId3Fk)
                     on o.QAAccuracyId equals o1.Id into j1
                     from s1 in j1.DefaultIfEmpty()

                     join o2 in _nlpClientInfo.GetAll() on o.ClientId equals o2.ClientId into j2
                     from s2 in j2.DefaultIfEmpty()

                     where o.NlpChatbotId == chatbotId && o.ClientId == clientId && o.NlpCreationTime > dt && (bUnreadOnly == false || (o.AgentReadTime == null || o.ClientReadTime == null))

                     select new
                     {
                         Message = new ChatbotMessageManagerMessageDto()
                         {
                             Id = o.Id,
                             ClientId = o.ClientId,
                             ChatbotId = o.NlpChatbotId,
                             Message = o.NlpMessage,
                             MessageType = o.NlpMessageType,
                             ReceiverRole = o.NlpReceiverRole,
                             SenderRole = o.NlpSenderRole,
                             SenderTime = o.NlpCreationTime,
                             AgentId = o.NlpAgentId,
                             AgentReadTime = o.AgentReadTime,
                             ClientReadTime = o.ClientReadTime,
                             AlternativeQuestion = o.AlternativeQuestion,

                             ClientChannel = s2.ClientChannel,
                             ConnectionProtocol = s2.ConnectionProtocol
                         },
                         QAAccuracyId = o.QAAccuracyId,
                         acc1 = s1.AnswerAcc1,
                         acc2 = s1.AnswerAcc2,
                         acc3 = s1.AnswerAcc3,
                         Answer1 = s1.AnswerId1Fk.Answer,
                         Answer2 = s1.AnswerId2Fk.Answer,
                         Answer3 = s1.AnswerId3Fk.Answer,
                         NlpCreationTime = o.NlpCreationTime
                     }).Distinct().OrderByDescending(e => e.NlpCreationTime).Take(30).Reverse().ToListAsync();

                //var nlpCbMessages = nlpCbMessagesQuery.Reverse();
                var nlpCbMessages = new List<ChatbotMessageManagerMessageDto>();

                foreach (var message in nlpCbMessagesQuery)
                {
                    if (message.acc1.HasValue && message.acc1.Value > 0.3 && message.Answer1.IsNullOrEmpty() == false)
                    {
                        message.Message.MessageDetails ??= new List<ChatbotMessageDetails>();
                        message.Message.MessageDetails.Add(new ChatbotMessageDetails()
                        {
                            Acc = message.acc1.Value,
                            Messages = await ReplaceCustomStringAsync(JsonToAnswer(message.Answer1), chatbotId)
                            //(JsonConvert.DeserializeObject<IList<string>>(message.Answer1), chatbotId)
                        });
                    }

                    if (message.acc2.HasValue && message.acc2.Value > 0.3 && message.Answer2.IsNullOrEmpty() == false)
                    {
                        message.Message.MessageDetails ??= new List<ChatbotMessageDetails>();
                        message.Message.MessageDetails.Add(new ChatbotMessageDetails()
                        {
                            Acc = message.acc2.Value,
                            Messages = await ReplaceCustomStringAsync(JsonToAnswer(message.Answer2), chatbotId)
                        });
                    }

                    if (message.acc3.HasValue && message.acc3.Value > 0.3 && message.Answer3.IsNullOrEmpty() == false)
                    {
                        message.Message.MessageDetails ??= new List<ChatbotMessageDetails>();
                        message.Message.MessageDetails.Add(new ChatbotMessageDetails()
                        {
                            Acc = message.acc3.Value,
                            Messages = await ReplaceCustomStringAsync(JsonToAnswer(message.Answer3), chatbotId)
                        });
                    }

                    NlpUserNameImage receiver = await GetNameImage(message.Message.ReceiverRole, message.Message.ReceiverRole == "client" ? message.Message.ClientId : message.Message.ReceiverRole == "chatbot" ? message.Message.ChatbotId : null, message.Message.AgentId, "");

                    message.Message.ReceiverName = receiver.Name;
                    message.Message.ReceiverImage = receiver.Image;
                    message.Message.ReceiverImage ??= "/Common/Images/default-profile-picture.png";

                    NlpUserNameImage sender = await GetNameImage(message.Message.SenderRole,
                    message.Message.SenderRole == "client" ? message.Message.ClientId : message.Message.SenderRole == "chatbot" ? message.Message.ChatbotId : null, message.Message.AgentId, message.Message.ClientChannel);
                    message.Message.SenderName = sender.Name;
                    message.Message.SenderImage = sender.Image;
                    message.Message.SenderImage ??= "/Common/Images/default-profile-picture.png";


                    nlpCbMessages.Add(message.Message);
                }

                return nlpCbMessages;
            }
        }


        public async Task<IList<ChatbotMessageManagerMessageDto>> GetMessagesByHttp(ChatbotMessageManagerMessageDto input)
        {
            //傳送未讀的Message至Agents跟Client
            IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(input.ChatbotId.Value, input.ClientId.Value, 7, true);

            //設為已讀
            if (input.ChatbotId != null && input.ClientId != null)
            {
                DateTime dt30 = Clock.Now.AddDays(-30);
                //var filteredNlpCbMessages = _nlpCbMessageRepository.GetAll()
                //            .Where(e => e.NlpChatbotId == input.ChatbotId.Value && e.ClientId == input.ClientId && e.NlpCreationTime > dt30 && (e.ClientReadTime == null || e.AlternativeQuestion != null));

                //foreach (var nlpCbMessage in filteredNlpCbMessages)
                //{
                //    nlpCbMessage.ClientReadTime = DateTime.Now;
                //    nlpCbMessage.AlternativeQuestion = null;
                //}
                //context.Items.Where(a => a.ItemId <= 500).BatchUpdateAsync(new Item { Description = "Updated" });

                await _nlpCbMessageRepository.BatchUpdateAsync(
                    e => new NlpCbMessage { AlternativeQuestion = null, ClientReadTime = Clock.Now },
                    e => e.NlpChatbotId == input.ChatbotId.Value && e.ClientId == input.ClientId && e.NlpCreationTime > dt30 && (e.ClientReadTime == null || e.AlternativeQuestion != null));
            }

            var clientMessages = messages.Where(e => e.ClientReadTime == null && e.ReceiverRole == "client").ToList();

            var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
            foreach (var message in clientMessages)
                message.FailedCount = chatroomStatus.IncorrectAnswerCount;

            if (chatroomStatus.WfState != Guid.Empty)
            {
                var workflowStatus = await GetNlpWorkflowStateInfo(chatroomStatus.WfState);
                if (workflowStatus != null)
                {
                    foreach (var message in clientMessages)
                    {
                        message.Workflow = workflowStatus.NlpWorkflowName;
                        message.WorkflowState = workflowStatus.StateName;
                    }
                }
            }

            return clientMessages;
        }


        public void SendErrorMessage(IClientProxy client, string errorMessage)
        {
            var dto = new ChatbotMessageManagerMessageDto()
            {
                Message = errorMessage,
            };

            _chatbotCommunicator.SendMessageToClient(client, "errorMessage", dto.ToDictionary());
        }

        private async Task SendAgesntsClientNonReadMessage(Guid chatbotId, Guid clientId, IList<ChatbotMessageManagerMessageDto> messages, bool sentClient)
        {
            //送至Client
            if (sentClient)
            {
                var connectionProtocol = (await GetChatroomStatus(chatbotId, clientId)).ConnectionProtocol;

                if (connectionProtocol == "signal-r" || connectionProtocol == "line" || connectionProtocol == "facebook" || connectionProtocol.IsNullOrEmpty() == true)
                {
                    var clientMessages = messages.Where(e => e.ClientReadTime == null && (e.SenderRole == "client" || e.ReceiverRole == "client")).ToList();
                    if (clientMessages.Count > 0)
                    {
                        if (connectionProtocol == "line")
                        {
                            try
                            {
                                var lineUser = _nlpLineUsersAppService.GetNlpLineUserDto(clientId);
                                var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
                                var bot = new isRock.LineBot.Bot(chatbot.LineToken);

                                foreach (var message in clientMessages)
                                    if (message.ReceiverRole == "client")
                                        bot.PushMessage(lineUser.UserId, new isRock.LineBot.TextMessage(StripHTML(message.Message)));

                                await OnClientSendReceipt(chatbotId, clientId);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        else if (connectionProtocol == "facebook")
                        {
                            try
                            {
                                var facebookUser = _nlpFacebookUsersAppService.GetNlpFacebookUserDto(clientId);
                                var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);

                                _clientMessenger ??= new ClientMessenger(chatbot.FacebookAccessToken, chatbot.FacebookSecretKey);

                                foreach (var message in clientMessages)
                                {
                                    if (message.ReceiverRole == "client")
                                    {
                                        var result = await _clientMessenger.SendMessageAsync(facebookUser.UserId, new ReflectSoftware.Facebook.Messenger.Common.Models.Client.TextMessage(StripHTML(message.Message)), ReflectSoftware.Facebook.Messenger.Common.Enums.NotificationType.Regular, null, null);
                                    }
                                }

                                await OnClientSendReceipt(chatbotId, clientId);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        else
                        {
                            var connection = (NlpClientConnection)_cacheManager.Get_NlpClientConnection_By_ChatbotId_ClientId(chatbotId, clientId);

                            if (connection != null)
                            {
                                var client = _onlineClientManager.GetByConnectionIdOrNull(connection.ConnectionId);
                                if (client != null)
                                    _chatbotCommunicator.SendMessageToClient(client, "receiveMessages", ChatbotMessageManagerMessageDto.ToDictionary(clientMessages));
                            }
                        }
                    }
                }
            }

            //送至Agents
            var agentMessages = messages.Where(e => e.AgentReadTime == null).ToList();
            if (agentMessages.Count > 0)
            {
                await SendMessageToChatroomAgents(chatbotId, clientId, "agentGetChatbotMessages", agentMessages);
            }
        }


        private async Task SendAgesntsSuggestedAnswers(Guid chatbotId, Guid clientId, IList<ChatbotMessageManagerMessageDto> messages)
        {
            if (messages.Count > 0)
                await SendMessageToChatroomAgents(chatbotId, clientId, "suggestedAnswers", messages);
        }

        private async Task DeferredSendAgentUnfoundMessageAnswer(Guid chatbotId, Guid clientId)
        {
            var errorMessage = await GetUnfoundMessageAsync(chatbotId, clientId, null, null, null);
            if (errorMessage == null)
                return;

            var chatroomStatus = await GetChatroomStatus(chatbotId, clientId);
            var errorMessageDto = new ChatbotMessageManagerMessageDto()
            {
                Id = Guid.NewGuid(),
                ClientId = errorMessage.ClientId,
                ChatbotId = errorMessage.NlpChatbotId,
                Message = null,
                MessageType = errorMessage.NlpMessageType,
                ReceiverRole = "agent",
                SenderRole = errorMessage.NlpSenderRole,
                SenderTime = errorMessage.NlpCreationTime,
                AgentId = errorMessage.NlpAgentId ?? chatroomStatus?.ChatroomAgents?[0]?.AgentId,
                AgentTenantId = errorMessage.TenantId,
                AgentReadTime = errorMessage.AgentReadTime,
                ClientReadTime = errorMessage.ClientReadTime,
                SuggestedAnswers = JsonConvert.SerializeObject(new string[] { errorMessage.NlpMessage })
            };

            _deferredSendMessageToChatroomAgent ??= new List<ChatbotMessageManagerMessageDto>();
            _deferredSendMessageToChatroomAgent.Add(errorMessageDto);
        }






        [DisableAuditing]
        public async Task OnClientSendReceipt(Guid chatbotId, Guid clientId)
        {
            //設Client已讀
            DateTime dt30 = Clock.Now.AddDays(-30);

            await _nlpCbMessageRepository.BatchUpdateAsync(
                e => new NlpCbMessage { AlternativeQuestion = null, ClientReadTime = Clock.Now },
                e => e.NlpChatbotId == chatbotId && e.ClientId == clientId && e.NlpCreationTime > dt30 && (e.ClientReadTime == null || e.AlternativeQuestion != null));
        }


        [DisableAuditing]
        public async Task AgentEnableResponseConfirm(ChatbotMessageManagerMessageDto input, bool enableResponseConfirm)
        {
            if (input.AgentId != AbpSession.UserId || input.AgentTenantId != AbpSession.TenantId || input.ChatbotId == null || input.ClientId == null ||
                IsGranted(AppPermissions.Pages_NlpChatbot_NlpCbAgentOperations_SendMessage) == false)
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

            var chatroom = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
            if (chatroom.UnreadMessageCount != 0 || chatroom.ResponseConfirmEnabled != enableResponseConfirm)
            {
                chatroom.UnreadMessageCount = 0;
                chatroom.ResponseConfirmEnabled = enableResponseConfirm;
                UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroom);
                SendChatroomStatusToAgents(input.AgentTenantId.Value, chatroom);
            }
        }







        public async Task AgentReconnect(ChatbotMessageManagerMessageDto input)
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

            var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
            if (chatroomStatus != null)
                SendChatroomStatusToAgents(input.AgentTenantId.Value, chatroomStatus);
        }


        [DisableAuditing]
        public async Task DisconnectNotification(string connectionId)
        {
            var clientConnection = (NlpClientConnection)_cacheManager.Get_NlpClientConnection_By_ConnectionId(connectionId);
            if (clientConnection != null)
            {
                await RemoveClientConnectionFromCache(clientConnection);

                var chatroomStatus = await GetChatroomStatus(clientConnection.ChatbotId, clientConnection.ClientId);
                if (chatroomStatus != null)
                {
                    var chatbot = _nlpChatbotFunction.GetChatbotDto(clientConnection.ChatbotId);
                    SendChatroomStatusToAgents(chatbot.TenantId, chatroomStatus);
                }

                return;
            }

            var agentConnection = (NlpAgentConnection)_cacheManager.Get_NlpAgentConnection_By_ConnectionId(connectionId);
            if (agentConnection != null)
            {
                await AddRemoveAgentFromChatroom(false, new NlpAgentInChatroom(agentConnection.AgentId, agentConnection.ChatbotId.Value, agentConnection.ClientId.Value), null);

                RemoveAgentConnectionFromCache(agentConnection);

                //var chatbot = _nlpChatbotFunction.GetChatbot(agentConnection.ChatbotId);
                var chatroomStatus = await GetChatroomStatus(agentConnection.ChatbotId.Value, agentConnection.ClientId.Value);
                SendChatroomStatusToAgents(agentConnection.AgentTenantId, chatroomStatus);

                return;
            }
        }


        [DisableAuditing]
        public bool IsValidChatroom(Guid chatbotId)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            if (chatbot == null)
                return false;
            return true;
        }







    }
}
