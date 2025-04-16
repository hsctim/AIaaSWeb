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

namespace AIaaS.Chatbot
{

    [RemoteService(false)]
    [DisableAuditing]
    public class ChatbotMessageManager : ApplicationService, IChatbotMessageManager
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

        /// <summary>
        /// Client User送Signal-r訊號至Chatbot
        /// </summary>
        /// <param name="input"></param>
        [DisableAuditing]
        public async Task ReceiveClientSignalRMessage(ChatbotMessageManagerMessageDto input)
        {
            //_lpCbWebApiClient.PrepareQueryPython();

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

            var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
            if (chatbot == null)
                throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "ChatbotId should be a valid guid.");

            if (input.ConnectionProtocol.IsNullOrEmpty() == false)
                await SetNlpClientInfoDtosCache(new NlpClientInfoDto(chatbot.TenantId, input.ClientId.Value, input.ConnectionProtocol, input.ClientIP, input.ClientChannel, input.ClientToken));

            var semaphoreSlim = await _nlpPolicyAppService.GetMessageSendQuotaSemaphoreSlim(chatbot.TenantId);

            InferenctSlime(semaphoreSlim);

            try
            {
                if ((await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut)) == false)
                    return;

                //取得Chatbot回覆
                input.MessageType ??= "text";
                input.SenderTime ??= Clock.Now;
                if (string.IsNullOrEmpty(input.SenderRole))
                    input.SenderRole = "chatbot";
                var nlpCbMessageExList = await ProcessReceiveMessage(input);

                var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
                if (chatroomStatus != null)
                {
                    if ((input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol) ||
                        (input.ClientChannel.IsNullOrEmpty() == false && chatroomStatus.ClientChannel != input.ClientChannel) ||
                        (input.ClientIP.IsNullOrEmpty() == false && chatroomStatus.ClientIP != input.ClientIP))
                    {
                        if (input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol)
                            chatroomStatus.ConnectionProtocol = input.ConnectionProtocol;

                        if (input.ClientChannel.IsNullOrEmpty() == false && chatroomStatus.ClientChannel != input.ClientChannel)
                            chatroomStatus.ClientChannel = input.ClientChannel;

                        if (input.ClientIP.IsNullOrEmpty() == false && chatroomStatus.ClientIP != input.ClientIP)
                            chatroomStatus.ClientIP = input.ClientIP;

                        UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroomStatus);
                    }

                    SendChatroomStatusToAgents(chatbot.TenantId, chatroomStatus);
                }

                //傳送未讀的Message至Agents跟Client
                IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true);

                await SendAgesntsClientNonReadMessage(chatbot.Id, input.ClientId.Value, messages, true);

                if (nlpCbMessageExList != null)
                {
                    foreach (var nlpCbMessageEx in nlpCbMessageExList)
                    {
                        if (nlpCbMessageEx != null && nlpCbMessageEx.SuggestedAnswers != null && nlpCbMessageEx.SuggestedAnswers.Count > 0)
                        {
                            var output = input;
                            output.ReceiverRole = "agent";
                            output.SenderRole = "chatbot";
                            output.Message = "";
                            output.SuggestedAnswers = JsonConvert.SerializeObject(nlpCbMessageEx.SuggestedAnswers);
                            output.AgentId ??= chatroomStatus.ChatroomAgents[0].AgentId;

                            var messageList = new List<ChatbotMessageManagerMessageDto>();
                            messageList.Add(output);

                            await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, messageList);
                        }
                    }
                }

                if (_deferredSendMessageToChatroomAgent != null)
                    await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, _deferredSendMessageToChatroomAgent);
            }
            finally
            {
                try
                {
                    semaphoreSlim?.Release();
                }
                catch (Exception)
                {
                }
            }

            //_lpCbWebApiClient.PrepareQueryPython();
        }

        [DisableAuditing]
        public async Task<IList<ChatbotMessageManagerMessageDto>> ReceiveClientHttpMessage(ChatbotMessageManagerMessageDto input)
        {
            List<NlpCbMessageEx> nlpCbMessageExList = null;
            SemaphoreSlim semaphoreSlim = null;

            //_lpCbWebApiClient.PrepareQueryPython();

            try
            {
                var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
                if (chatbot == null)
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "ChatbotId should be a valid guid.");

                semaphoreSlim = await _nlpPolicyAppService.Get_GetMessageQuotaSemaphoreSlim(chatbot.TenantId);

                InferenctSlime(semaphoreSlim);

                if ((await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut)) == false)
                    return new List<ChatbotMessageManagerMessageDto>();


                //取得Chatbot回覆
                input.MessageType ??= "text";
                input.SenderTime ??= Clock.Now;
                input.SenderRole = "client";

                nlpCbMessageExList = await ProcessReceiveMessage(input);

                if (input.ConnectionProtocol.IsNullOrEmpty() == false)
                    await SetNlpClientInfoDtosCache(new NlpClientInfoDto(chatbot.TenantId, input.ClientId.Value, input.ConnectionProtocol, input.ClientIP, input.ClientChannel, input.ClientToken));

                var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
                if (chatroomStatus != null)
                {
                    if ((input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol) ||
                        (input.ClientChannel.IsNullOrEmpty() == false && chatroomStatus.ClientChannel != input.ClientChannel) ||
                        (input.ClientIP.IsNullOrEmpty() == false && chatroomStatus.ClientIP != input.ClientIP))
                    {
                        if (input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol)
                            chatroomStatus.ConnectionProtocol = input.ConnectionProtocol;

                        if (input.ClientChannel.IsNullOrEmpty() == false && chatroomStatus.ClientChannel != input.ClientChannel)
                            chatroomStatus.ClientChannel = input.ClientChannel;

                        if (input.ClientIP.IsNullOrEmpty() == false && chatroomStatus.ClientIP != input.ClientIP)
                            chatroomStatus.ClientIP = input.ClientIP;

                        UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroomStatus);
                    }

                    SendChatroomStatusToAgents(chatbot.TenantId, chatroomStatus);
                }

                //傳送未讀的Message至Agents跟Client
                IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true);

                await SendAgesntsClientNonReadMessage(chatbot.Id, input.ClientId.Value, messages, false);

                if (nlpCbMessageExList != null)
                {
                    foreach (var nlpCbMessageEx in nlpCbMessageExList)
                    {
                        if (nlpCbMessageEx != null && nlpCbMessageEx.SuggestedAnswers != null && nlpCbMessageEx.SuggestedAnswers.Count > 0)
                        {
                            var output = input;
                            output.ReceiverRole = "agent";
                            output.SenderRole = "chatbot";
                            output.Message = "";
                            output.SuggestedAnswers = JsonConvert.SerializeObject(nlpCbMessageEx.SuggestedAnswers);
                            output.AgentId ??= chatroomStatus.ChatroomAgents[0].AgentId;


                            var messageList = new List<ChatbotMessageManagerMessageDto>();
                            messageList.Add(output);

                            await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, messageList);
                        }
                    }
                }

                //設為已讀
                if (input.ChatbotId != null && input.ClientId != null)
                {
                    DateTime dt30 = Clock.Now.AddDays(-30);

                    await _nlpCbMessageRepository.BatchUpdateAsync(
                        e => new NlpCbMessage { AlternativeQuestion = null, ClientReadTime = Clock.Now },
                        e => e.NlpChatbotId == input.ChatbotId.Value && e.ClientId == input.ClientId && e.NlpCreationTime > dt30 && (e.ClientReadTime == null || e.AlternativeQuestion != null));
                }

                var clientMessages = messages.Where(e => e.ClientReadTime == null && e.ReceiverRole == "client").ToList();

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
            finally
            {
                try
                {
                    semaphoreSlim?.Release();
                }
                catch (Exception)
                {
                }

            }
        }

        [DisableAuditing]
        public async Task<IList<ChatbotMessageManagerMessageDto>> ReceiveClientLineMessage(ChatbotMessageManagerMessageDto input)
        {
            List<NlpCbMessageEx> nlpCbMessageExList = null;
            SemaphoreSlim semaphoreSlim = null;

            //_lpCbWebApiClient.PrepareQueryPython();

            try
            {
                //傳送ChatroomStatus至Agents
                var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
                if (chatbot == null)
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "ChatbotId should be a valid guid.");

                semaphoreSlim = await _nlpPolicyAppService.GetMessageSendQuotaSemaphoreSlim(chatbot.TenantId);

                InferenctSlime(semaphoreSlim);

                if ((await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut)) == false)
                    return new List<ChatbotMessageManagerMessageDto>();

                //if (await _nlpPolicy.IsExceedingMaxAnswerSendCount(chatbot.TenantId))
                //    return null;

                //取得Chatbot回覆
                input.MessageType ??= "text";
                input.SenderTime ??= Clock.Now;
                input.SenderRole = "client";
                input.ConnectionProtocol = "line";
                input.ClientChannel = "line";

                nlpCbMessageExList = await ProcessReceiveMessage(input);

                if (input.ConnectionProtocol.IsNullOrEmpty() == false)
                {
                    await SetNlpClientInfoDtosCache(new NlpClientInfoDto()
                    {
                        TenantId = chatbot.TenantId,
                        ClientId = input.ClientId.Value,
                        ConnectionProtocol = input.ConnectionProtocol,
                        ClientChannel = input.ClientChannel,
                        UpdatedTime = Clock.Now,
                    });
                }

                var lineUser = _nlpLineUsersAppService.GetNlpLineUserDto(input.ClientId.Value);

                var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
                if (chatroomStatus != null)
                {
                    if ((input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol) ||
                        (input.ClientChannel.IsNullOrEmpty() == false && chatroomStatus.ClientChannel != input.ClientChannel) ||
                        (input.ClientIP.IsNullOrEmpty() == false && chatroomStatus.ClientIP != input.ClientIP))
                    {
                        if (input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol)
                            chatroomStatus.ConnectionProtocol = input.ConnectionProtocol;

                        if (input.ClientChannel.IsNullOrEmpty() == false && chatroomStatus.ClientChannel != input.ClientChannel)
                            chatroomStatus.ClientChannel = input.ClientChannel;

                        if (input.ClientIP.IsNullOrEmpty() == false && chatroomStatus.ClientIP != input.ClientIP)
                            chatroomStatus.ClientIP = input.ClientIP;

                        UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroomStatus);
                    }

                    SendChatroomStatusToAgents(chatbot.TenantId, chatroomStatus);
                }

                //傳送未讀的Message至Agents跟Client
                IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true);

                await SendAgesntsClientNonReadMessage(chatbot.Id, input.ClientId.Value, messages, false);

                if (nlpCbMessageExList != null)
                {
                    foreach (var nlpCbMessageEx in nlpCbMessageExList)
                    {
                        if (nlpCbMessageEx != null && nlpCbMessageEx.SuggestedAnswers != null && nlpCbMessageEx.SuggestedAnswers.Count > 0)
                        {
                            var output = input;
                            output.ReceiverRole = "agent";
                            output.SenderRole = "chatbot";
                            output.Message = "";
                            output.SuggestedAnswers = JsonConvert.SerializeObject(nlpCbMessageEx.SuggestedAnswers);
                            output.AgentId ??= chatroomStatus.ChatroomAgents[0].AgentId;

                            var messageList = new List<ChatbotMessageManagerMessageDto>();
                            messageList.Add(output);

                            await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, messageList);
                        }
                    }
                }

                var clientMessages = messages.Where(e => e.ClientReadTime == null && e.ReceiverRole == "client").ToList();

                return clientMessages;
            }
            finally
            {
                try
                {
                    semaphoreSlim?.Release();
                }
                catch (Exception)
                {
                }

            }
        }

        [DisableAuditing]
        public async Task ReceiveClientFacebookMessage(ChatbotMessageManagerMessageDto input)
        {
            List<NlpCbMessageEx> nlpCbMessageExList = null;
            SemaphoreSlim semaphoreSlim = null;

            //_lpCbWebApiClient.PrepareQueryPython();

            try
            {
                //傳送ChatroomStatus至Agents
                var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
                if (chatbot == null)
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "ChatbotId should be a valid guid.");

                //if (await _nlpPolicy.IsExceedingMaxAnswerSendCount(chatbot.TenantId))
                //    return;
                semaphoreSlim = await _nlpPolicyAppService.GetMessageSendQuotaSemaphoreSlim(chatbot.TenantId);

                InferenctSlime(semaphoreSlim);

                if ((await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut)) == false)
                    return;

                //取得Chatbot回覆
                input.MessageType ??= "text";
                input.SenderTime ??= Clock.Now;
                input.SenderRole = "client";
                input.ConnectionProtocol = "facebook";
                input.ClientChannel = "facebook";

                nlpCbMessageExList = await ProcessReceiveMessage(input);

                if (input.ConnectionProtocol.IsNullOrEmpty() == false)
                {
                    await SetNlpClientInfoDtosCache(new NlpClientInfoDto()
                    {
                        TenantId = chatbot.TenantId,
                        ClientId = input.ClientId.Value,
                        ConnectionProtocol = input.ConnectionProtocol,
                        ClientChannel = input.ClientChannel,
                        UpdatedTime = Clock.Now,
                    });
                }

                var lineUser = _nlpLineUsersAppService.GetNlpLineUserDto(input.ClientId.Value);

                var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
                if (chatroomStatus != null)
                {
                    if ((input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol) ||
                        (input.ClientChannel.IsNullOrEmpty() == false && chatroomStatus.ClientChannel != input.ClientChannel) ||
                        (input.ClientIP.IsNullOrEmpty() == false && chatroomStatus.ClientIP != input.ClientIP))
                    {
                        if (input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol)
                            chatroomStatus.ConnectionProtocol = input.ConnectionProtocol;

                        if (input.ClientChannel.IsNullOrEmpty() == false && chatroomStatus.ClientChannel != input.ClientChannel)
                            chatroomStatus.ClientChannel = input.ClientChannel;

                        if (input.ClientIP.IsNullOrEmpty() == false && chatroomStatus.ClientIP != input.ClientIP)
                            chatroomStatus.ClientIP = input.ClientIP;

                        UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroomStatus);
                    }

                    SendChatroomStatusToAgents(chatbot.TenantId, chatroomStatus);
                }

                //傳送未讀的Message至Agents跟Client
                IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true);

                var clientMessages = messages.Where(e => e.ClientReadTime == null && e.ReceiverRole == "client").ToList();
                await SendAgesntsClientNonReadMessage(chatbot.Id, input.ClientId.Value, clientMessages, true);

                if (nlpCbMessageExList != null)
                {
                    foreach (var nlpCbMessageEx in nlpCbMessageExList)
                    {
                        if (nlpCbMessageEx != null && nlpCbMessageEx.SuggestedAnswers != null && nlpCbMessageEx.SuggestedAnswers.Count > 0)
                        {
                            var output = input;
                            output.ReceiverRole = "agent";
                            output.SenderRole = "chatbot";
                            output.Message = "";
                            output.SuggestedAnswers = JsonConvert.SerializeObject(nlpCbMessageEx.SuggestedAnswers);
                            output.AgentId ??= chatroomStatus.ChatroomAgents[0].AgentId;

                            var messageList = new List<ChatbotMessageManagerMessageDto>();
                            messageList.Add(output);

                            await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, messageList);
                        }
                    }
                }

                foreach (var message in clientMessages)
                {
                    if (message.AlternativeQuestion.IsNullOrEmpty() == false)
                    {
                        List<ReflectSoftware.Facebook.Messenger.Common.Models.Button> listButtons = null;

                        var questions = JsonConvert.DeserializeObject<string[]>(message.AlternativeQuestion);
                        foreach (var question in questions)
                        {
                            listButtons ??= new List<ReflectSoftware.Facebook.Messenger.Common.Models.Button>();
                            listButtons.Add(new PostbackButton()
                            {
                                Title = question,
                                Payload = question
                            });
                        }

                        //var text = "Hey there welcome to Hubster! How can we help you today?";
                        var attachmentMessage = new AttachmentMessage
                        {
                            Attachment = new ButtonTemplateAttachment(chatbot.AlternativeQuestion, listButtons)
                        };

                        _clientMessenger ??= new ClientMessenger(chatbot.FacebookAccessToken, chatbot.FacebookSecretKey);

                        var facebookUser = _nlpFacebookUsersAppService.GetNlpFacebookUserDto(message.ClientId.Value);
                        var package = await _clientMessenger.GetJSONRenderedAsync(facebookUser.UserId, attachmentMessage);
                        var result = await _clientMessenger.SendMessageAsync(facebookUser.UserId, attachmentMessage);
                    }
                }
            }
            finally
            {
                try
                {
                    semaphoreSlim?.Release();
                }
                catch (Exception)
                {
                }

            }

            //_lpCbWebApiClient.PrepareQueryPython();
        }

        private async Task<List<NlpCbMessageEx>> ProcessReceiveMessage(ChatbotMessageManagerMessageDto input)
        {
            if (input.Message.Length > 256)
                throw new UserFriendlyException(ChatErrorCode.Error_InvalidInputParameter, "Invalid input parameter");

            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
            if (nlpChatbot == null || nlpChatbot.Disabled)
                throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "ChatbotId should be a valid guid.");
            //return null;

            input.SenderTime ??= Clock.Now;

            List<NlpCbMessageEx> nlpCbMessageExList = null;
            NlpCbMessageEx nlpCbMessageEx = new NlpCbMessageEx();

            try
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MustHaveTenant, AbpDataFilters.MayHaveTenant))
                {
                    nlpCbMessageEx.NlpCbMessage = await _nlpCbMessageRepository.InsertAsync(new NlpCbMessage()
                    {
                        TenantId = nlpChatbot.TenantId,
                        ClientId = input.ClientId,
                        NlpChatbotId = input.ChatbotId,
                        NlpMessage = input.Message,
                        NlpMessageType = input.MessageType,
                        NlpCreationTime = input.SenderTime.Value,
                        NlpSenderRole = input.SenderRole,
                        NlpReceiverRole = input.ReceiverRole,
                        NlpAgentId = input.AgentId,
                    });

                    //CurrentUnitOfWork.SaveChanges();

                    //加入Message到Chatroom
                    if (input.SenderRole == "client" || input.ReceiverRole == "client")
                    {
                        await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage()
                        {
                            IsClientSent = input.SenderRole == "client" ? true : false,
                            Message = input.Message
                        });
                    }

                    //送至Chatbot AI
                    if (input.ReceiverRole == "chatbot" && input.ChatbotId.HasValue && input.MessageType == "text")
                    {
                        try
                        {
                            nlpCbMessageExList = await GetChatbotReplyMessage(nlpChatbot, input.ClientId.Value, nlpCbMessageEx.NlpCbMessage);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.ToString(), ex);

                            var chatroomStatus = await GetChatroomStatus(nlpChatbot.Id, input.ClientId.Value);

                            if (input.SenderRole == "agent" || (chatroomStatus.ResponseConfirmEnabled == true && chatroomStatus.ChatroomAgents.Count > 0))
                            {
                                await DeferredSendAgentUnfoundMessageAnswer(input.ChatbotId.Value, input.ClientId.Value);
                                return nlpCbMessageExList;
                            }
                            else
                            {
                                throw;
                            }
                        }

                        //加入Message到Chatroom

                        if (nlpCbMessageExList != null)
                        {
                            foreach (var msg in nlpCbMessageExList)
                            {
                                if (msg != null && nlpCbMessageEx.NlpCbMessage != null && msg.NlpCbMessage.NlpReceiverRole == "client")
                                    await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage() { IsClientSent = false, Message = msg.NlpCbMessage.NlpMessage });

                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"An error occured while ProcessReceiveMessage", ex);

                nlpCbMessageEx.NlpCbMessage = await SaveUnfoundMessage(nlpChatbot.Id, input.ClientId.Value, null , null, null);
                if (nlpCbMessageEx?.NlpCbMessage != null)
                    await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage() { IsClientSent = false, Message = input.Message });
            }

            return nlpCbMessageExList;
        }


        private async Task<List<NlpCbMessageEx>> GetChatbotReplyMessage(NlpChatbotDto nlpChatbot, Guid clientId, NlpCbMessage nlpCbMessage)
        {
            var nlpCbMessageExList = new List<NlpCbMessageEx>();
            var allPredictMessages = new List<AllPredictMessages>();

            //var threshold_predict = nlpChatbot.PredThreshold;
            //var threshold_suggestion = nlpChatbot.SuggestionThreshold;

            //NlpCbMessageEx nlpCbMessageEx = null;
            var inputMessage = nlpCbMessage.NlpMessage.Trim();

            //var questionWebAPITask = _nlpCbDictionariesFunction.GetQuestionSegmentsHashForComparison(nlpChatbot.TenantId, nlpChatbot.Id, nlpChatbot.Language, inputMessage);

            var chatroomStatus = await GetChatroomStatus(nlpChatbot.Id, clientId);

            //if (chatroomStatus.WfState != Guid.Empty)
            //threshold_predict = nlpChatbot.WSPredThreshold;

            //var threshold_same = threshold_predict / 2;      //檢查Segment是否相同，但accu要大於threadhold_same才檢查

            var workflowStatus = await GetNlpWorkflowStateInfo(chatroomStatus.WfState);

            //先檢查在流程內的WorkflowState
            if (nlpCbMessage.NlpSenderRole != "agent" && workflowStatus != null)
            {
                //先檢查在流程內的WorkflowState
                //var message1 = _nlpCbDictionariesFunction.PrepareSynonymString(nlpChatbot.TenantId, nlpChatbot.Id, nlpChatbot.Language, (workflowStatus.Id, inputMessage));

                var predictResult1 = await ChatbotPredict(nlpChatbot.Id, inputMessage, workflowStatus.Id);
                if (predictResult1.errorCode == "success" && predictResult1.result != null && predictResult1.result.Any())
                {
                    foreach (var item in predictResult1.result)
                    {
                        //只加入相同workflowstate的回應
                        var nlpQADto = await GetNlpQADtofromNNID(nlpChatbot.Id, item.nnid);
                        if (nlpQADto == null)
                            continue;

                        if (nlpQADto.CurrentWfState != null && nlpQADto.CurrentWfState != Guid.Empty && nlpQADto.CurrentWfState.Value == workflowStatus.Id)
                        {
                            item.QaId = nlpQADto.Id;

                            allPredictMessages.Add(new AllPredictMessages()
                            {
                                ChatbotPredictInput = new (Guid?, string)[] { (workflowStatus.Id, inputMessage) },
                                ChatbotPredictResult = item,
                                InputState = workflowStatus == null ? Guid.Empty : workflowStatus.Id,
                                NlpQADto = nlpQADto,
                                inPredictionThreshold = item.probability > nlpChatbot.WSPredThreshold,
                                inWorkflowState = true,
                                inSuggestionThreshold = item.probability > nlpChatbot.SuggestionThreshold
                            });
                        }
                    }
                }

                //再檢查在流程內的Workflow，workflow內有多個workflowstates

                predictResult1 = await ChatbotPredict(nlpChatbot.Id, inputMessage, workflowStatus.NlpWorkflowId);
                if (predictResult1.errorCode == "success" && predictResult1.result != null && predictResult1.result.Any())
                {
                    foreach (var item in predictResult1.result)
                    {
                        var nlpQADto = await GetNlpQADtofromNNID(nlpChatbot.Id, item.nnid);
                        if (nlpQADto == null)
                            continue;

                        if (nlpQADto.CurrentWfState != null && nlpQADto.CurrentWfState != Guid.Empty && nlpQADto.CurrentWfState.Value == workflowStatus.NlpWorkflowId)
                        {
                            if (nlpQADto.CurrentWfState == workflowStatus.NlpWorkflowId)
                            {
                                item.QaId = nlpQADto.Id;

                                //只加入相同workflow的回應
                                allPredictMessages.Add(new AllPredictMessages()
                                {
                                    //ChatbotPredictInput = message1,
                                    ChatbotPredictInput = new (Guid?, string)[] { (workflowStatus.NlpWorkflowId, nlpCbMessage.NlpMessage) },
                                    ChatbotPredictResult = item,
                                    InputState = workflowStatus == null ? Guid.Empty : workflowStatus.Id,
                                    NlpQADto = nlpQADto,
                                    inPredictionThreshold = item.probability > nlpChatbot.WSPredThreshold,
                                    inWorkflow = true,
                                    inSuggestionThreshold = item.probability > nlpChatbot.SuggestionThreshold

                                });
                            }
                        }
                    }
                }
            }

            //非流程或在流程但可以問非流程問題
            if (workflowStatus == null || workflowStatus.ResponseNonWorkflowAnswer)
            {
                //var message2 = _nlpCbDictionariesFunction.PrepareSynonymString(nlpChatbot.TenantId, nlpChatbot.Id, nlpChatbot.Language, (Guid.Empty, nlpCbMessage.NlpMessage));

                var predictResult2 = await ChatbotPredict(nlpChatbot.Id, nlpCbMessage.NlpMessage, Guid.Empty);
                if (predictResult2.errorCode == "success" && predictResult2.result != null && predictResult2.result.Any())
                {
                    foreach (var item in predictResult2.result)
                    {
                        //只加入相同workflowstatus==null的回應
                        var nlpQADto = await GetNlpQADtofromNNID(nlpChatbot.Id, item.nnid);
                        if (nlpQADto == null)
                            continue;

                        if (nlpQADto.CurrentWfState == Guid.Empty || nlpQADto.CurrentWfState == null)
                        {
                            item.QaId = nlpQADto.Id;

                            allPredictMessages.Add(new AllPredictMessages()
                            {
                                ChatbotPredictInput = new (Guid?, string)[] { (Guid.Empty, nlpCbMessage.NlpMessage) },
                                ChatbotPredictResult = item,
                                InputState = workflowStatus == null ? Guid.Empty : workflowStatus.Id,
                                NlpQADto = nlpQADto,
                                inPredictionThreshold = (workflowStatus != null ? item.probability > nlpChatbot.WSPredThreshold : item.probability > nlpChatbot.PredThreshold),
                                inSuggestionThreshold = item.probability > nlpChatbot.SuggestionThreshold
                            });
                        }
                    }
                }
            }

            //Distinct 去掉多餘且可能性低的相同NNID回應
            var orderPredictMessages = allPredictMessages.Where(e => e.ChatbotPredictResult.nnid != 0)
                .OrderByDescending(e => e.ChatbotPredictResult.probability > .99)
                .ThenByDescending(e => e.ChatbotPredictResult.probability > .95)
                .ThenByDescending(e => e.inPredictionThreshold)
                .ThenByDescending(e => e.ChatbotPredictResult.probability * (e.inWorkflowState ? 1.1 : 1.0) * (e.inWorkflow ? 1.05 : 1.0));

            var distinctPredictMessages = new List<AllPredictMessages>();
            foreach (var item in orderPredictMessages)
            {
                if (distinctPredictMessages.Any(e => e.ChatbotPredictResult.nnid == item.ChatbotPredictResult.nnid) == false)
                {
                    distinctPredictMessages.Add(item);

                    if (distinctPredictMessages.Count >= 3)
                        break;
                }
            }

            //設定連續無法回答問題的數目
            if (nlpCbMessage.NlpSenderRole != "agent")
            {
                chatroomStatus.IncorrectAnswerCount = (distinctPredictMessages.Count > 0 && distinctPredictMessages.First().inPredictionThreshold) ? 0 : chatroomStatus.IncorrectAnswerCount + 1;
            }


            //設定新的流程狀態，若命中問題
            if (distinctPredictMessages.Count > 0 && distinctPredictMessages.First().inPredictionThreshold)
            {
                var nlpQADto = distinctPredictMessages.First().NlpQADto;
                if (nlpQADto.NextWfState != null && nlpQADto.NextWfState != NlpWorkflowStateConsts.WfsKeepCurrent)
                {
                    chatroomStatus.WfState = (nlpQADto.NextWfState == null) ? Guid.Empty : nlpQADto.NextWfState.Value;
                    Debug.WriteLine("WorkflowState : " + chatroomStatus.WfState.ToString());
                }
            }


            //設定無法命中的回應，當錯誤>=1次或>=3次時設定
            string PredictionErrorMessage = null;
            if (workflowStatus != null && chatroomStatus.IncorrectAnswerCount >= 1)
            {
                if (chatroomStatus.IncorrectAnswerCount >= 3)
                {
                    var nlpWfsOp = await GetNlpWfsFalsePredictionOpDto(nlpChatbot.Id, clientId, workflowStatus.Outgoing3FalseOp);
                    if (nlpWfsOp != null)
                    {
                        PredictionErrorMessage = nlpWfsOp.ResponseMsg;
                        chatroomStatus.WfState = nlpWfsOp.NextStatus;
                        Debug.WriteLine("WorkflowState : " + chatroomStatus.WfState.ToString());
                    }
                }
                else if (chatroomStatus.IncorrectAnswerCount >= 1)
                {
                    var nlpWfsOp = await GetNlpWfsFalsePredictionOpDto(nlpChatbot.Id, clientId, workflowStatus.OutgoingFalseOp);
                    if (nlpWfsOp != null)
                    {
                        PredictionErrorMessage = nlpWfsOp.ResponseMsg;
                        chatroomStatus.WfState = nlpWfsOp.NextStatus;
                        Debug.WriteLine("WorkflowState : " + chatroomStatus.WfState.ToString());
                    }
                }
            }

            var nlpCbQAAccuracy = await SaveNlpCbQAAccuracy(nlpCbMessage, distinctPredictMessages);

            //若是Agent監控
            if (nlpCbMessage.NlpSenderRole == "agent" || (chatroomStatus.ResponseConfirmEnabled == true && chatroomStatus.ChatroomAgents.Count > 0))
            {
                if (PredictionErrorMessage.IsNullOrEmpty() == false)
                {
                    var sendNlpCbMessage = new NlpCbMessageEx(new NlpCbMessage()
                    {
                        TenantId = nlpChatbot.TenantId,
                        ClientId = clientId,
                        NlpChatbotId = nlpChatbot.Id,
                        NlpMessage = PredictionErrorMessage,
                        NlpMessageType = "text.workflow.error",
                        NlpCreationTime = Clock.Now,
                        NlpSenderRole = "chatbot",
                        NlpReceiverRole = "agent",
                        NlpAgentId = nlpCbMessage.NlpAgentId,
                        AlternativeQuestion = null,
                        QAAccuracyId = null
                    });

                    await _nlpCbMessageRepository.InsertAsync(sendNlpCbMessage.NlpCbMessage);
                    //CurrentUnitOfWork.SaveChanges();
                    nlpCbMessageExList.Add(sendNlpCbMessage);
                }

                var suggestedAnswers = new List<string>();

                var unfoundMessage = (await GetUnfoundMessageWithoutGPTAsync(nlpChatbot.Id, clientId, inputMessage, null, nlpCbQAAccuracy?.Id))?.NlpMessage;

                if (unfoundMessage.IsNullOrEmpty() == false)
                    suggestedAnswers.Add(unfoundMessage);


                foreach (var result in distinctPredictMessages)
                {
                    if (result.inSuggestionThreshold)
                    {
                        try
                        {
                            var nlpQADto = result.NlpQADto;
                            var answers = JsonConvert.DeserializeObject<string[]>(nlpQADto.Answer);

                            foreach (var answer in answers)
                                suggestedAnswers.Add(await ReplaceCustomStringAsync(answer, nlpChatbot.Id));
                        }
                        catch (Exception ex)
                        {
                            Logger.Fatal(ex.ToString(), ex);
                        }
                    }
                    else
                        break;
                }

                nlpCbMessageExList.Add(new NlpCbMessageEx(new NlpCbMessage()
                {
                    TenantId = nlpChatbot.TenantId,
                    ClientId = clientId,
                    NlpChatbotId = nlpChatbot.Id,
                    NlpMessage = "",
                    NlpMessageType = chatroomStatus.WfState == Guid.Empty ? "text" : "text.workflow",
                    NlpCreationTime = Clock.Now,
                    NlpSenderRole = "chatbot",
                    NlpReceiverRole = "agent",
                    NlpAgentId = nlpCbMessage.NlpAgentId,
                    AlternativeQuestion = null,
                    QAAccuracyId = nlpCbQAAccuracy?.Id
                }, suggestedAnswers
                ));

                return nlpCbMessageExList;
            }
            else
            {
                //User至Chatbot 或 Agent端不監控，直接由Chatbot回應至User

                if (PredictionErrorMessage.IsNullOrEmpty() == false)
                {
                    var sendNlpCbMessage2 = new NlpCbMessageEx(new NlpCbMessage()
                    {
                        TenantId = nlpChatbot.TenantId,
                        ClientId = clientId,
                        NlpChatbotId = nlpChatbot.Id,
                        NlpMessage = PredictionErrorMessage,
                        NlpMessageType = "text.workflow.error",
                        NlpCreationTime = Clock.Now,
                        NlpSenderRole = "chatbot",
                        NlpReceiverRole = "client",
                        NlpAgentId = null,
                        AlternativeQuestion = null,
                        QAAccuracyId = null
                    });

                    await _nlpCbMessageRepository.InsertAsync(sendNlpCbMessage2.NlpCbMessage);
                    //CurrentUnitOfWork.SaveChanges();
                    nlpCbMessageExList.Add(sendNlpCbMessage2);
                }

                List<string> alternativeQuestion = null;


                var firstIndex = 0;
                if (distinctPredictMessages.FirstOrDefault().inPredictionThreshold)
                    firstIndex = 1;

                foreach (var result in distinctPredictMessages.Skip(firstIndex))
                {
                    //if (result.ChatbotPredictResult.probability >= threshold_high)
                    //    break;

                    if (result.inSuggestionThreshold)
                    {
                        try
                        {
                            var nlpQADto = result.NlpQADto;
                            var questions = nlpQADto.GetQuestionList();
                            alternativeQuestion ??= new List<string>();
                            alternativeQuestion.Add(await ReplaceCustomStringAsync(questions.First(), nlpChatbot.Id));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                if (distinctPredictMessages.First().inPredictionThreshold == false && nlpCbMessage.NlpSenderRole != "agent")
                {
                    if (PredictionErrorMessage.IsNullOrEmpty() == true || (PredictionErrorMessage.IsNullOrEmpty() == false && workflowStatus != null && workflowStatus.DontResponseNonWorkflowErrorAnswer == false))
                    {
                        var unfoundMessage = await SaveUnfoundMessage(nlpChatbot.Id, clientId, inputMessage, alternativeQuestion, nlpCbQAAccuracy?.Id);

                        if (unfoundMessage != null)
                            nlpCbMessageExList.Add(new NlpCbMessageEx(unfoundMessage));
                    }

                    return nlpCbMessageExList;
                }

                //回傳預測答案及QaId
                PredictedQAMessage output = new PredictedQAMessage();

                if (AbpSession.TenantId == 1)
                {
                    foreach (var i in distinctPredictMessages)
                    {
                        output ??= new PredictedQAMessage();

                        if (output.Message.IsNullOrWhiteSpace() == false && output.Message.Length > 0)
                            output.Message += "<br>";

                        output.QaId = distinctPredictMessages.First().ChatbotPredictResult.QaId;

                        output.Message += (100.0 * i.ChatbotPredictResult.probability).ToString("N2") + "___" + i.ChatbotPredictResult.nnid.ToString() + "___" + (await GetAnswerFromNNIDRepetition(nlpChatbot.Id, i.ChatbotPredictResult.nnid));
                    }
                }
                else
                {
                    output.Message = await GetAnswerFromNNIDRepetition(nlpChatbot.Id, distinctPredictMessages.First().ChatbotPredictResult.nnid);

                    output.QaId = distinctPredictMessages.First().ChatbotPredictResult.QaId;
                }

                //更新變數
                if (output.Message.IsNullOrEmpty() == false)
                {
                    output.Message = await ReplaceCustomStringAsync(output.Message, nlpChatbot.Id);
                    output.Message = await ChatGPT(nlpChatbot.Id, inputMessage, output.Message);
                }

                if (output.Message.IsNullOrEmpty())
                {
                    var unfoundMessage = await SaveUnfoundMessage(nlpChatbot.Id, clientId,  inputMessage, alternativeQuestion, nlpCbQAAccuracy?.Id);

                    if (unfoundMessage != null)
                        nlpCbMessageExList.Add(new NlpCbMessageEx(unfoundMessage));

                    return nlpCbMessageExList;
                }

                NlpCbMessage sendNlpCbMessage = new NlpCbMessage()
                {
                    TenantId = nlpChatbot.TenantId,
                    ClientId = clientId,
                    NlpChatbotId = nlpChatbot.Id,
                    NlpMessage = output.Message.Substring(0, Math.Min(output.Message.Length, 1024)),
                    QAId = output.QaId,
                    NlpMessageType = chatroomStatus.WfState == Guid.Empty ? "text" : "text.workflow",
                    NlpCreationTime = Clock.Now,
                    NlpSenderRole = "chatbot",
                    NlpReceiverRole = "client",
                    NlpAgentId = null,
                    AlternativeQuestion = (alternativeQuestion == null) ? null : JsonConvert.SerializeObject(alternativeQuestion),
                    QAAccuracyId = nlpCbQAAccuracy?.Id
                };

                await _nlpCbMessageRepository.InsertAsync(sendNlpCbMessage);
                //CurrentUnitOfWork.SaveChanges();

                nlpCbMessageExList.Add(new NlpCbMessageEx(sendNlpCbMessage));
                return nlpCbMessageExList;
            }
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


        private async Task<NlpCbMessage> SaveUnfoundMessage(Guid chatbotId, Guid clientId, string question, List<string> alternativeQuestion, Guid? qaAccuracyId)
        {
            var nlpCbMessage = await GetUnfoundMessageAsync(chatbotId, clientId, question, alternativeQuestion, qaAccuracyId);

            if (nlpCbMessage == null)
                return null;

            await _nlpCbMessageRepository.InsertAsync(nlpCbMessage);
            //CurrentUnitOfWork.SaveChanges();
            return nlpCbMessage;
        }

        private void SendChatroomStatusToAgents(int tenantId, NlpChatroomStatus chatroomStatus)
        {
            if (chatroomStatus.LatestMessages != null && chatroomStatus.LatestMessages.Count > 0)
                _chatbotCommunicator.SendMessageToTenant(tenantId, "updateChatroomStatus", chatroomStatus.ToDictionary());
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

        private async Task<NlpQADto> GetNlpQADtofromNNID(Guid chatbotId, int nnid)
        {
            Debug.Assert(chatbotId != Guid.Empty);

            var nlpQADto =
                (NlpQADto)_cacheManager.Get_NlpQADtoFromNNID(chatbotId, nnid)
                ??
               (NlpQADto)_cacheManager.Set_NlpQADtoFromNNID(chatbotId, nnid,
               ObjectMapper.Map<NlpQADto>(await _nlpQARepository.FirstOrDefaultAsync(e => e.NlpChatbotId == chatbotId && e.NNID == nnid)));

            return nlpQADto;
        }

        protected async Task<string> GetAnswerFromNNIDRepetition(Guid chatbotId, int nnid)
        {
            List<int> nnidLists;

            var nnidRepetition = await GetAnswerFromNNIDRepetition(chatbotId);

            if (nnidRepetition != null && nnidRepetition.ContainsKey(nnid) == true)
                nnidLists = nnidRepetition[nnid].ToList();
            else
            {
                nnidLists = new List<int>();
                nnidLists.Add(nnid);
            }

            List<string> answers = new List<string>();

            foreach (var nnidListItem in nnidLists)
            {
                var nlpQADto = await GetNlpQADtofromNNID(chatbotId, nnidListItem);
                string jsonAnswers = nlpQADto?.Answer;

                if (jsonAnswers != null)
                {
                    answers.AddRange(JsonToAnswer(jsonAnswers));
                }
            }

            if (answers.Count == 0)
                return null;

            Random random = new Random();
            int start = random.Next(0, answers.Count);
            return answers[start];
        }

        protected async Task<string> ChatGPT(Guid chatbotId, string question, string answer)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            if (chatbot == null || chatbot.Disabled || chatbot.EnableOPENAI == (int)NlpChatbotConsts.EnableGPTType.Disabled )
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


        protected async Task<Dictionary<int, int[]>> GetAnswerFromNNIDRepetition(Guid chatbotId)
        {
            var nnidRepetition = (Dictionary<int, int[]>)_cacheManager.Get_NlpNNIDRepetition(chatbotId);

            if (nnidRepetition == null)
            {
                var data = await _nlpCbTrainingDataRepository.FirstOrDefaultAsync(e => e.NlpChatbotId == chatbotId);
                if (data != null && string.IsNullOrEmpty(data.NlpNNIDRepetition) == false)
                {
                    nnidRepetition = JsonConvert.DeserializeObject<Dictionary<int, int[]>>(data.NlpNNIDRepetition);
                    _cacheManager.Set_NlpNNIDRepetition(chatbotId, nnidRepetition);
                }
            }

            return nnidRepetition;
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

        /// <summary>
        /// 取得Client, Agent或Chatbot的頭像或名字
        /// </summary>
        /// <param name="role"></param>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task<NlpUserNameImage> GetNameImage(string role, Guid? id, long? userId, string channel)
        {
            NlpUserNameImage nameImage = new NlpUserNameImage();

            switch (role)
            {
                case "client":
                    nameImage.Name = "";
                    nameImage.Image = "/Common/Images/default-profile-picture.png";

                    if (channel == "line")
                    {
                        var lineUser = _nlpLineUsersAppService.GetNlpLineUserDto(id.Value);
                        if (lineUser != null)
                        {
                            if (lineUser.UserName.IsNullOrEmpty() == false)
                                nameImage.Name = lineUser.UserName;
                            if (lineUser.PictureUrl.IsNullOrEmpty() == false)
                                nameImage.Image = lineUser.PictureUrl;
                        }
                    }
                    else if (channel == "facebook")
                    {
                        var facebookUser = _nlpFacebookUsersAppService.GetNlpFacebookUserDto(id.Value);
                        if (facebookUser != null)
                        {
                            if (facebookUser.UserName.IsNullOrEmpty() == false)
                                nameImage.Name = facebookUser.UserName;
                            if (facebookUser.PictureUrl.IsNullOrEmpty() == false)
                                nameImage.Image = facebookUser.PictureUrl;
                        }
                    }


                    break;
                case "chatbot":
                    var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(id.Value);
                    if (nlpChatbot != null)
                    {
                        nameImage.Name = nlpChatbot.Name;
                        nameImage.Image = "/Chatbot/ProfilePicture/" + nlpChatbot.ChatbotPictureId.ToString();
                    }
                    else
                    {
                        nameImage.Name = "/Chatbot/ProfilePicture";
                        nameImage.Image = "";
                    }
                    break;
                case "agent":
                    var agent = await GetAgentNamePicture(userId.Value);
                    nameImage.Name = agent.AgentName;
                    nameImage.Image = "/Chatbot/GetProfilePictureById/" + agent.AgentPictureId.ToString();
                    break;
            }
            return nameImage;
        }


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


        public async Task<NlpClientInfoDto> GetNlpClientInfoDtosCache(int tenantId, Guid clientId)
        {
            if (__nlpClientInfoDtoCache != null && __nlpClientInfoDtoCache.TenantId == tenantId && __nlpClientInfoDtoCache.ClientId == clientId)
                return __nlpClientInfoDtoCache;

            __nlpClientInfoDtoCache = (NlpClientInfoDto)_cacheManager.Get_NlpClientInfoDto(tenantId, clientId);
            if (__nlpClientInfoDtoCache != null)
                return __nlpClientInfoDtoCache;

            var nlpClientInfo = await _nlpClientInfo.FirstOrDefaultAsync(e => e.TenantId == tenantId && e.ClientId == clientId);

            if (nlpClientInfo == null)
                return null;

            __nlpClientInfoDtoCache = ObjectMapper.Map<NlpClientInfoDto>(nlpClientInfo);
            _cacheManager.Set_NlpClientInfoDto(tenantId, clientId, __nlpClientInfoDtoCache);
            return __nlpClientInfoDtoCache;
        }

        public async Task<NlpClientInfoDto> SetNlpClientInfoDtosCache(NlpClientInfoDto dto)
        {
            if (dto != null && __nlpClientInfoDtoCache != null && dto.isSame(__nlpClientInfoDtoCache) == true)
                return dto;

            __nlpClientInfoDtoCache = dto;
            _cacheManager.Set_NlpClientInfoDto(dto.TenantId, dto.ClientId, dto);

            var nlpClientInfo = await _nlpClientInfo.FirstOrDefaultAsync(e => e.TenantId == dto.TenantId && e.ClientId == dto.ClientId);

            if (nlpClientInfo != null)
            {
                dto.Id = nlpClientInfo.Id;
                dto.UpdatedTime = Clock.Now;

                ObjectMapper.Map(dto, nlpClientInfo);
            }
            else
            {
                await _nlpClientInfo.InsertAsync(ObjectMapper.Map<NlpClientInfo>(dto));
            }

            return dto;
        }

        [DisableAuditing]
        public async Task ReceiveClientReceipt(ChatbotMessageManagerMessageDto input)
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

            var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value);
            if (chatroomStatus != null && input.ConnectionProtocol.IsNullOrEmpty() == false && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol)
            {
                chatroomStatus.ConnectionProtocol = input.ConnectionProtocol;
                UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroomStatus);
            }

            DateTime dt30 = Clock.Now.AddDays(-30);
            if (input.ChatbotId == null || input.ClientId == null)
                return;

            //var filteredNlpCbMessages = _nlpCbMessageRepository.GetAll()
            //            .Where(e => e.NlpChatbotId == input.ChatbotId.Value && e.ClientId == input.ClientId && e.NlpCreationTime > dt30 && (e.ClientReadTime == null || e.AlternativeQuestion != null));

            //foreach (var nlpCbMessage in filteredNlpCbMessages)
            //{
            //    nlpCbMessage.ClientReadTime = DateTime.Now;
            //    nlpCbMessage.AlternativeQuestion = null;
            //}

            await _nlpCbMessageRepository.BatchUpdateAsync(
                e => new NlpCbMessage { AlternativeQuestion = null, ClientReadTime = Clock.Now },
                e => e.NlpChatbotId == input.ChatbotId.Value && e.ClientId == input.ClientId && e.NlpCreationTime > dt30 && (e.ClientReadTime == null || e.AlternativeQuestion != null));
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

        public async Task<ChatroomWorkflowInfo> SetChatbotWorkflowState(SetChatroomWorkflow workflow)
        {
            var chatroomStatus = await GetChatroomStatus(workflow.ChatbotId, workflow.ClientId);
            if (chatroomStatus == null)
                return null;

            if (workflow.WorkflowName.IsNullOrEmpty() || workflow.WorkflowName.IsNullOrWhiteSpace() || workflow.WorkflowStateName.IsNullOrEmpty() || workflow.WorkflowStateName.IsNullOrWhiteSpace())
                chatroomStatus.WfState = Guid.Empty;
            else
            {
                Guid wfpStatus = await _nlpWorkflowStateRepository.GetAll()
                     .Include(e => e.NlpWorkflowFk)
                     .Where(e => e.StateName == workflow.WorkflowStateName && e.NlpWorkflowFk.Name == workflow.WorkflowName && e.NlpWorkflowFk.NlpChatbotId == workflow.ChatbotId).Select(e => e.Id).FirstOrDefaultAsync();

                if (chatroomStatus.WfState != wfpStatus)
                {
                    chatroomStatus.WfState = wfpStatus;
                    chatroomStatus.IncorrectAnswerCount = 0;
                }
            }

            UpdateChatroomStatusCache(workflow.ChatbotId, workflow.ClientId, chatroomStatus);
            return await GetChatbotWorkflowState(new NlpChatroom(workflow.ChatbotId, workflow.ClientId));
        }

        public async Task<ChatroomWorkflowInfo> GetChatbotWorkflowState(NlpChatroom chatroomId)
        {
            var chatroomStatus = await GetChatroomStatus(chatroomId.ChatbotId, chatroomId.ClientId);
            if (chatroomStatus == null)
                return null;

            //var WfsStatus = chatroomStatus.WfState;
            var workflowStatus = await GetNlpWorkflowStateInfo(chatroomStatus.WfState);

            chatroomStatus.WfState = workflowStatus?.Id ?? Guid.Empty;

            var data = new ChatroomWorkflowInfo()
            {
                ChatbotId = chatroomStatus.ChatbotId,
                ClientId = chatroomStatus.ClientId,
                WorkflowId = workflowStatus?.NlpWorkflowId ?? null,
                WorkflowName = workflowStatus?.NlpWorkflowName ?? null,
                WorkflowStateId = workflowStatus?.Id ?? null,
                WorkflowStateName = workflowStatus?.StateName ?? null,
            };

            if (data.WorkflowName.IsNullOrEmpty() || data.WorkflowName.IsNullOrWhiteSpace())
                data.WorkflowName = null;

            if (data.WorkflowStateName.IsNullOrEmpty() || data.WorkflowStateName.IsNullOrWhiteSpace())
                data.WorkflowStateName = null;

            return data;
        }


        [DisableAuditing]
        public bool IsValidChatroom(Guid chatbotId)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            if (chatbot == null)
                return false;
            return true;
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

        private void RemoveAgentConnectionFromCache(NlpAgentConnection nlpAgentConnection)
        {
            if (nlpAgentConnection != null)
            {
                _cacheManager.Remove_NlpAgentConnection_By_ChatbotId_ClientId_UserId(nlpAgentConnection.ChatbotId.Value, nlpAgentConnection.ClientId.Value, nlpAgentConnection.AgentId);

                _cacheManager.Remove_NlpAgentConnection_By_ConnectionId(nlpAgentConnection.ConnectionId);
            }
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

        private void AddNlpAgentConnectionCache(NlpAgentConnection agent)
        {
            _cacheManager.Set_NlpAgentConnection_By_ChatbotId_ClientId_UserId(agent.ChatbotId.Value, agent.ClientId.Value, agent.AgentId, agent);
            _cacheManager.Set_NlpAgentConnection_By_ConnectionId(agent.ConnectionId, agent);
        }

        private async Task<NlpChatroomAgent> GetAgentNamePicture(long userId)
        {
            if (__userLoginInfoDtoCache == null || __userLoginInfoDtoCache.Id != userId)
                __userLoginInfoDtoCache = (UserLoginInfoDto)_cacheManager.Get_UserLoginInfoDto(userId);

            if (AbpSession.UserId.HasValue && __userLoginInfoDtoCache == null)
            {
                var info = await _sessionAppService.GetCurrentLoginInformations();
                __userLoginInfoDtoCache = info.User;
            }

            if (__userLoginInfoDtoCache != null)
            {
                Guid? profilePictureId = null;
                try
                {
                    profilePictureId = Guid.Parse(__userLoginInfoDtoCache.ProfilePictureId);
                }
                catch (Exception)
                {
                }

                return new NlpChatroomAgent()
                {
                    AgentId = userId,
                    AgentName = __userLoginInfoDtoCache.Name,
                    AgentPictureId = profilePictureId
                };
            }
            else
            {
                return new NlpChatroomAgent()
                {
                    AgentId = userId,
                    AgentName = "",
                    AgentPictureId = null
                };
            }
        }


        private async Task<NlpWorkflowStateInfo> GetNlpWorkflowStateInfo(Guid workflowStateId)
        {
            if (workflowStateId == Guid.Empty)
                return null;

            if (__nlpWorkflowStateInfoCache == null || __nlpWorkflowStateInfoCache.Id != workflowStateId)
                __nlpWorkflowStateInfoCache = (NlpWorkflowStateInfo)_cacheManager.Get_NlpWorkflowStates(workflowStateId);

            if (__nlpWorkflowStateInfoCache == null)
            {
                var nlpWorkflowStateInfo = await (from o in _nlpWorkflowStateRepository.GetAll().Include(e => e.NlpWorkflowFk).Where(e => e.Id == workflowStateId)
                                                  select new NlpWorkflowStateInfo()
                                                  {
                                                      Id = o.Id,
                                                      ResponseNonWorkflowAnswer = o.ResponseNonWorkflowAnswer,
                                                      DontResponseNonWorkflowErrorAnswer = o.DontResponseNonWorkflowErrorAnswer,
                                                      NlpWorkflowId = o.NlpWorkflowId,
                                                      Outgoing3FalseOp = o.Outgoing3FalseOp,
                                                      OutgoingFalseOp = o.OutgoingFalseOp,
                                                      StateInstruction = o.StateInstruction,
                                                      StateName = o.StateName,
                                                      NlpWorkflowName = o.NlpWorkflowFk.Name
                                                  }).FirstOrDefaultAsync();

                __nlpWorkflowStateInfoCache = (NlpWorkflowStateInfo)_cacheManager.Set_NlpWorkflowStates(workflowStateId, nlpWorkflowStateInfo);
            }

            return __nlpWorkflowStateInfoCache;
        }

        public async Task<NlpCbGetChatbotPredictResult> ChatbotPredict(Guid chatbotId, string question, Guid? workflowState)
        {
            NlpCbGetChatbotPredictResult result = await _lpCbWebApiClient.ChatbotPredict(chatbotId, question, workflowState);

            return result;
        }



        public async Task<NlpCbGetChatbotPredictResult> ChatbotPredictBySimilarity(Guid chatbotId, string question, Guid? workflowState)
        {
            NlpCbGetChatbotPredictResult result = await _lpCbWebApiClient.ChatbotPredict(chatbotId, question, workflowState);

            if (result.errorCode != "success")
                return result;

            var predictQuestions = new List<IList<string>>();
            foreach (var resultItem in result.result)
            {
                var nlpQADto = await GetNlpQADtofromNNID(chatbotId, resultItem.nnid);

                var state1 = workflowState;
                var state2 = Guid.Empty;

                if (nlpQADto != null && nlpQADto.CurrentWfState != null)
                    state2 = nlpQADto.CurrentWfState.Value;

                if (nlpQADto == null || state1 != state2)
                {
                    predictQuestions.Add(null);
                    continue;
                }
                else
                {
                    IList<string> questions = null;
                    try
                    {
                        questions = JsonConvert.DeserializeObject<IList<string>>(nlpQADto.Question);
                    }
                    catch (Exception)
                    {
                    }

                    predictQuestions.Add(questions);
                }
            }

            var similarities = await _lpCbWebApiClient.GetSentencesSimilarityAsync(question, predictQuestions);
            var similarities2 = similarities.similarities.ToArray();

            int i = 0;
            foreach (var resultItem in result.result)
            {
                resultItem.probability = similarities2[i];
                i++;
            }

            result.result = result.result.OrderByDescending(e => e.probability).ToArray();

            return result;
        }

        public async Task<NlpWfsFalsePredictionOpDto> GetNlpWfsFalsePredictionOpDto(Guid chatbotId, Guid clientId, string json)
        {
            try
            {
                var value = JsonConvert.DeserializeObject<NlpWfsFalsePredictionOpDto>(json);

                if (value.NextStatus == NlpWorkflowStateConsts.WfsKeepCurrent)
                {
                    var chatroomInfo = await GetChatroomStatus(chatbotId, clientId);
                    value.NextStatus = chatroomInfo.WfState;
                }

                return value;

            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<string> ReplaceCustomStringAsync(string text, Guid chatbotId)
        {
            string result = text;

            if (text.Contains("${") == true && text.Contains("}") == true)
            {

                if (text.Contains("${Chatbot.Name}", StringComparison.OrdinalIgnoreCase))
                {
                    var nlpChatbotName = _nlpChatbotFunction.GetChatbotName(chatbotId);
                    text = text.Replace("${Chatbot.Name}", nlpChatbotName);
                }

                var list = NlpDataParserHelper.ParserJson(text);
                StringBuilder sb = new StringBuilder();

                foreach (var item in list)
                {
                    if (item.name == "text")
                        sb.Append(await _externalCustomData.GetCustomDataAsync2(text));
                    else if (item.name == "json")
                        sb.Append(await _externalCustomData.GetCustomDataAsync((string)item.value));
                }

                result= sb.ToString();
            }

            //result = await _openAIClient.Chat(chatbotId, result);

            return result;
        }

        private async Task<IList<string>> ReplaceCustomStringAsync(IList<string> texts, Guid chatbotId)
        {
            var newList = new List<string>();

            foreach (var text in texts)
                newList.Add(
                    //await ChatGPT(chatbotId
                    await ReplaceCustomStringAsync(text, chatbotId));

            return newList;
        }

        private string StripHTML(string input)
        {
            if (input.IsNullOrEmpty())
                return input;

            input = input.Replace("<br>", "\n").Replace("<BR>", "\n");
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        private static void InferenctSlime(SemaphoreSlim slim, int Count = 1)
        {
            if (slim == null)
                return;

            _ = Task.Run(async () =>
            {
                for (int n = 0; n < Count; n++)
                {
                    try
                    {
                        await slim.WaitAsync(_SemaphoreSlimWaitTimeOut);
                        await Task.Delay(1000);
                    }
                    finally
                    {
                        slim.Release();
                    }
                }
            });
        }

        //json to Answer
        private IList<string> JsonToAnswer(string json)
        {
            if (json == null || json.IsNullOrEmpty())
            {
                return null;
            }

            try
            {
                var obj = JsonConvert.DeserializeObject<CbAnswerSet[]>(json);

                var stringList = new List<string>();

                foreach (var item in obj)
                {
                    if (item.GPT == true)
                    {
                        stringList.Add("[GPT]" + item.Answer);
                    }
                    else
                    {
                        stringList.Add(item.Answer);
                    }
                }

                return stringList;
            }
            catch (Exception)
            {
            }

            try
            {
                return JsonConvert.DeserializeObject<IList<string>>(json);
            }
            catch (Exception)
            {
                return null;
            }

        }
    }
}
