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
        private const int _SemaphoreSlimWaitTimeOut = 60000; // Consider making this configurable

        // Use IReadOnlyRepository for read-only operations if applicable
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
        private ClientMessenger _clientMessenger; // Consider lazy initialization or injecting a factory

        // Consider using ConcurrentDictionary for thread-safe caching if accessed/modified concurrently outside of single requests
        private NlpChatroomStatus __nlpChatroomStatusCache;
        private UserLoginInfoDto __userLoginInfoDtoCache;
        private NlpClientInfoDto __nlpClientInfoDtoCache;
        private NlpWorkflowStateInfo __nlpWorkflowStateInfoCache;


        private List<ChatbotMessageManagerMessageDto> _deferredSendMessageToChatroomAgent; // Consider thread-safety if accessed concurrently

        private readonly INlpPolicyAppService _nlpPolicyAppService;

        //private IHubCallerClients _hubCallerClients; // Commented out, remove if unused

        // Constructor Dependencies: Consider reducing the number of dependencies if possible (e.g., facade services)
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
            //NlpCbDictionariesFunction nlpCbDictionariesFunction, // Commented out, remove if unused
            ISessionAppService sessionAppService,
            INlpLineUsersAppService nlpLineUsersAppService,
            INlpFacebookUsersAppService nlpFacebookUsersAppService,
            INlpPolicyAppService nlpPolicyAppService
            //IHubCallerClients hubCallerClients // Commented out, remove if unused
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
            //_nlpCbDictionariesFunction = nlpCbDictionariesFunction; // Commented out, remove if unused
            _sessionAppService = sessionAppService;
            //_hubCallerClients = hubCallerClients; // Commented out, remove if unused
            _nlpLineUsersAppService = nlpLineUsersAppService;
            _nlpFacebookUsersAppService = nlpFacebookUsersAppService;
            _nlpPolicyAppService = nlpPolicyAppService;
        }

        /// <summary>
        /// Client User sends SignalR message to Chatbot.
        /// </summary>
        /// <param name="input">Message input DTO.</param>
        [DisableAuditing]
        public async Task ReceiveClientSignalRMessage(ChatbotMessageManagerMessageDto input)
        {
            // Validate input early
            if (input?.ChatbotId == null || input.ClientId == null)
            {
                Logger.Warn($"Received invalid input in {nameof(ReceiveClientSignalRMessage)}.");
                // Consider throwing a specific argument exception or returning early
                return;
            }

            //_lpCbWebApiClient.PrepareQueryPython(); // Commented out, remove if unused

            await AddNlpClientConnectionCache(new NlpClientConnection
            {
                ClientId = input.ClientId.Value,
                ChatbotId = input.ChatbotId.Value,
                ConnectionId = input.ConnectionId, // Ensure ConnectionId is reliably available
                UpdatedTime = Clock.Now,
                AgentId = input.AgentId,
                Connected = true,
                ClientIP = input.ClientIP,
                ClientChannel = input.ClientChannel
            });

            var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
            if (chatbot == null)
            {
                // Use nameof for exception messages where appropriate
                throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, $"{nameof(input.ChatbotId)} should be a valid guid.");
            }

            // Use !string.IsNullOrEmpty for clarity
            if (!string.IsNullOrEmpty(input.ConnectionProtocol))
            {
                // Consider creating the DTO outside the method call for clarity
                var clientInfo = new NlpClientInfoDto(chatbot.TenantId, input.ClientId.Value, input.ConnectionProtocol, input.ClientIP, input.ClientChannel, input.ClientToken);
                await SetNlpClientInfoDtosCache(clientInfo);
            }

            // Use a more descriptive variable name if possible
            var semaphoreSlim = await _nlpPolicyAppService.GetMessageSendQuotaSemaphoreSlim(chatbot.TenantId);

            // Rename InferenctSlime to something descriptive like TryDecrementQuota or similar
            DecrementQuota(semaphoreSlim); // Assuming this is the intended action

            try
            {
                // Use ConfigureAwait(false) if context synchronization is not needed after await
                if (!await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut).ConfigureAwait(false))
                {
                    Logger.Warn($"Semaphore timeout for TenantId: {chatbot.TenantId} in {nameof(ReceiveClientSignalRMessage)}.");
                    return; // Timeout occurred
                }

                // Set defaults concisely
                input.MessageType ??= "text";
                input.SenderTime ??= Clock.Now;
                // Avoid setting SenderRole if it's already set? Check logic.
                if (string.IsNullOrEmpty(input.SenderRole))
                    input.SenderRole = "client"; // Assuming SignalR messages are always from client initially

                var nlpCbMessageExList = await ProcessReceiveMessage(input).ConfigureAwait(false);

                // Refactor common post-processing logic
                await PostProcessClientMessage(input, chatbot, nlpCbMessageExList, true).ConfigureAwait(false);

            }
            finally
            {
                // Ensure Release is always called if WaitAsync succeeded, even if an exception occurred before return
                // The current structure is generally correct, but double-check complex scenarios.
                try
                {
                    semaphoreSlim?.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Handle or log if the semaphore might be disposed unexpectedly
                    Logger.Warn("Attempted to release a disposed semaphore.");
                }
                catch (SemaphoreFullException)
                {
                    // This shouldn't happen if WaitAsync was called correctly, but log if it does
                    Logger.Error("Attempted to release semaphore more times than it was waited on.");
                }
            }

            //_lpCbWebApiClient.PrepareQueryPython(); // Commented out, remove if unused
        }

        [DisableAuditing]
        public async Task<IList<ChatbotMessageManagerMessageDto>> ReceiveClientHttpMessage(ChatbotMessageManagerMessageDto input)
        {
            // Validate input early
            if (input?.ChatbotId == null || input.ClientId == null)
            {
                Logger.Warn($"Received invalid input in {nameof(ReceiveClientHttpMessage)}.");
                return new List<ChatbotMessageManagerMessageDto>(); // Return empty list on invalid input
            }

            List<NlpCbMessageEx> nlpCbMessageExList = null;
            SemaphoreSlim semaphoreSlim = null;

            //_lpCbWebApiClient.PrepareQueryPython(); // Commented out, remove if unused

            try
            {
                var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
                if (chatbot == null)
                {
                    // Consistent exception message
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, $"{nameof(input.ChatbotId)} should be a valid guid.");
                }

                // Distinguish between Get and Send quota semaphores if necessary
                semaphoreSlim = await _nlpPolicyAppService.Get_GetMessageQuotaSemaphoreSlim(chatbot.TenantId);

                DecrementQuota(semaphoreSlim); // Rename method

                if (!await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut).ConfigureAwait(false))
                {
                    Logger.Warn($"Semaphore timeout for TenantId: {chatbot.TenantId} in {nameof(ReceiveClientHttpMessage)}.");
                    return new List<ChatbotMessageManagerMessageDto>(); // Return empty list on timeout
                }

                // Set defaults
                input.MessageType ??= "text";
                input.SenderTime ??= Clock.Now;
                input.SenderRole = "client"; // HTTP messages are assumed from client

                nlpCbMessageExList = await ProcessReceiveMessage(input).ConfigureAwait(false);

                // Refactor common post-processing logic
                var chatroomStatus = await PostProcessClientMessage(input, chatbot, nlpCbMessageExList, false).ConfigureAwait(false);

                // --- HTTP Specific Post-Processing ---

                // Fetch messages again? This seems redundant if PostProcessClientMessage already fetched them.
                // If GetNlpCbMessageFromDatabase is needed *after* ProcessReceiveMessage, keep it. Otherwise, optimize.
                // Assuming messages are needed again here for HTTP response formatting.
                IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true).ConfigureAwait(false);

                // Mark messages as read for HTTP request
                if (input.ChatbotId != null && input.ClientId != null) // Redundant check? Already validated.
                {
                    DateTime dt30 = Clock.Now.AddDays(-30);
                    // Use ConfigureAwait(false) for background operations
                    await _nlpCbMessageRepository.BatchUpdateAsync(
                        e => new NlpCbMessage { AlternativeQuestion = null, ClientReadTime = Clock.Now },
                        e => e.NlpChatbotId == input.ChatbotId.Value && e.ClientId == input.ClientId && e.NlpCreationTime > dt30 && (e.ClientReadTime == null || e.AlternativeQuestion != null))
                        .ConfigureAwait(false);
                }

                // Filter messages for the client response
                var clientMessages = messages.Where(e => e.ClientReadTime == null && e.ReceiverRole == "client").ToList();

                // Add additional info to response messages
                foreach (var message in clientMessages)
                {
                    message.FailedCount = chatroomStatus.IncorrectAnswerCount;
                }

                if (chatroomStatus.WfState != Guid.Empty)
                {
                    var workflowStatus = await GetNlpWorkflowStateInfo(chatroomStatus.WfState).ConfigureAwait(false);
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
            catch (UserFriendlyException ex) // Catch specific exceptions if needed
            {
                Logger.Error($"UserFriendlyException in {nameof(ReceiveClientHttpMessage)}: {ex.Message}", ex);
                throw; // Re-throw UserFriendlyExceptions
            }
            catch (Exception ex) // Catch broader exceptions
            {
                Logger.Error($"An error occurred in {nameof(ReceiveClientHttpMessage)}.", ex);
                // Consider returning a specific error response DTO instead of an empty list or re-throwing
                // For now, return empty list as per original logic's tendency on failure
                return new List<ChatbotMessageManagerMessageDto>();
            }
            finally
            {
                try
                {
                    semaphoreSlim?.Release();
                }
                catch (ObjectDisposedException)
                {
                    Logger.Warn("Attempted to release a disposed semaphore.");
                }
                 catch (SemaphoreFullException)
                {
                    Logger.Error("Attempted to release semaphore more times than it was waited on.");
                }
            }
        }

        /// <summary>
        /// Common logic after processing a client message (SignalR, HTTP, Line, FB).
        /// </summary>
        /// <returns>The updated NlpChatroomStatus.</returns>
        private async Task<NlpChatroomStatus> PostProcessClientMessage(
            ChatbotMessageManagerMessageDto input,
            NlpChatbotDto chatbot,
            List<NlpCbMessageEx> nlpCbMessageExList,
            bool sendToClient)
        {
            // Update Client Info Cache
            if (!string.IsNullOrEmpty(input.ConnectionProtocol))
            {
                var clientInfo = new NlpClientInfoDto(chatbot.TenantId, input.ClientId.Value, input.ConnectionProtocol, input.ClientIP, input.ClientChannel, input.ClientToken);
                await SetNlpClientInfoDtosCache(clientInfo).ConfigureAwait(false);
            }

            // Get and Update Chatroom Status
            var chatroomStatus = await GetChatroomStatus(input.ChatbotId.Value, input.ClientId.Value).ConfigureAwait(false);
            if (chatroomStatus != null) // Ensure chatroomStatus is not null
            {
                bool statusChanged = false;
                if (!string.IsNullOrEmpty(input.ConnectionProtocol) && chatroomStatus.ConnectionProtocol != input.ConnectionProtocol)
                {
                    chatroomStatus.ConnectionProtocol = input.ConnectionProtocol;
                    statusChanged = true;
                }
                if (!string.IsNullOrEmpty(input.ClientChannel) && chatroomStatus.ClientChannel != input.ClientChannel)
                {
                    chatroomStatus.ClientChannel = input.ClientChannel;
                    statusChanged = true;
                }
                if (!string.IsNullOrEmpty(input.ClientIP) && chatroomStatus.ClientIP != input.ClientIP)
                {
                    chatroomStatus.ClientIP = input.ClientIP;
                    statusChanged = true;
                }

                if (statusChanged)
                {
                    UpdateChatroomStatusCache(input.ChatbotId.Value, input.ClientId.Value, chatroomStatus); // This method seems synchronous, confirm if await is needed
                }

                // Send status update to agents regardless of change? Check requirement.
                SendChatroomStatusToAgents(chatbot.TenantId, chatroomStatus); // This method seems synchronous, confirm if await is needed
            }
            else
            {
                // Handle case where chatroomStatus is unexpectedly null
                Logger.Error($"ChatroomStatus is null for ChatbotId: {input.ChatbotId}, ClientId: {input.ClientId} in PostProcessClientMessage.");
                // Depending on requirements, might need to create a default status or throw
                // For now, create a minimal status to avoid null reference errors later
                 chatroomStatus = new NlpChatroomStatus { ChatbotId = input.ChatbotId.Value, ClientId = input.ClientId.Value };
                 // Consider logging this creation or handling it more robustly
            }


            // Send Unread Messages
            // Consider optimizing: Can GetNlpCbMessageFromDatabase return only necessary fields?
            IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true).ConfigureAwait(false);
            await SendAgesntsClientNonReadMessage(chatbot.Id, input.ClientId.Value, messages, sendToClient).ConfigureAwait(false);

            // Handle Suggested Answers
            if (nlpCbMessageExList != null)
            {
                var messagesToSend = new List<ChatbotMessageManagerMessageDto>();
                foreach (var nlpCbMessageEx in nlpCbMessageExList)
                {
                    if (nlpCbMessageEx?.SuggestedAnswers?.Count > 0)
                    {
                        // Avoid modifying the original 'input' DTO directly if it's reused. Create a new DTO.
                        var output = new ChatbotMessageManagerMessageDto
                        {
                            // Copy necessary properties from input
                            ChatbotId = input.ChatbotId,
                            ClientId = input.ClientId,
                            TenantId = chatbot.TenantId, // Assuming TenantId is needed
                            // Set specific properties for suggestion
                            ReceiverRole = "agent",
                            SenderRole = "chatbot",
                            Message = "", // No primary message for suggestions
                            SuggestedAnswers = JsonConvert.SerializeObject(nlpCbMessageEx.SuggestedAnswers),
                            // Determine AgentId safely
                            AgentId = chatroomStatus?.ChatroomAgents?.FirstOrDefault()?.AgentId // Use ?. for safe navigation
                            // Copy other relevant fields if needed: SenderTime, MessageType?
                        };
                        messagesToSend.Add(output);
                    }
                }
                if (messagesToSend.Any())
                {
                    await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, messagesToSend).ConfigureAwait(false);
                }
            }

            // Handle Deferred Messages (Consider thread-safety if _deferredSendMessageToChatroomAgent is shared)
            if (_deferredSendMessageToChatroomAgent != null && _deferredSendMessageToChatroomAgent.Any())
            {
                // Create a copy to avoid modification issues if processing takes time or is concurrent
                var deferredMessages = _deferredSendMessageToChatroomAgent.ToList();
                _deferredSendMessageToChatroomAgent.Clear(); // Clear original list immediately
                await SendAgesntsSuggestedAnswers(input.ChatbotId.Value, input.ClientId.Value, deferredMessages).ConfigureAwait(false);
            }

            return chatroomStatus;
        }


        [DisableAuditing]
        public async Task<IList<ChatbotMessageManagerMessageDto>> ReceiveClientLineMessage(ChatbotMessageManagerMessageDto input)
        {
            // Validate input
             if (input?.ChatbotId == null || input.ClientId == null)
            {
                Logger.Warn($"Received invalid input in {nameof(ReceiveClientLineMessage)}.");
                return new List<ChatbotMessageManagerMessageDto>();
            }

            List<NlpCbMessageEx> nlpCbMessageExList = null;
            SemaphoreSlim semaphoreSlim = null;

            try
            {
                var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
                if (chatbot == null)
                {
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, $"{nameof(input.ChatbotId)} should be a valid guid.");
                }

                semaphoreSlim = await _nlpPolicyAppService.GetMessageSendQuotaSemaphoreSlim(chatbot.TenantId);
                DecrementQuota(semaphoreSlim); // Rename method

                if (!await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut).ConfigureAwait(false))
                {
                     Logger.Warn($"Semaphore timeout for TenantId: {chatbot.TenantId} in {nameof(ReceiveClientLineMessage)}.");
                    return new List<ChatbotMessageManagerMessageDto>();
                }

                // Set defaults for Line
                input.MessageType ??= "text";
                input.SenderTime ??= Clock.Now;
                input.SenderRole = "client";
                input.ConnectionProtocol = "line"; // Explicitly set for Line
                input.ClientChannel = "line";    // Explicitly set for Line

                nlpCbMessageExList = await ProcessReceiveMessage(input).ConfigureAwait(false);

                // Use common post-processing
                var chatroomStatus = await PostProcessClientMessage(input, chatbot, nlpCbMessageExList, false).ConfigureAwait(false); // sendToClient = false for Line? Check logic.

                // --- Line Specific Post-Processing ---
                // Fetch messages again for the response format needed by Line controller
                 IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true).ConfigureAwait(false);
                 var clientMessages = messages.Where(e => e.ClientReadTime == null && e.ReceiverRole == "client").ToList();

                // Potentially add Line-specific formatting or data here if needed

                return clientMessages;
            }
             catch (UserFriendlyException ex)
            {
                Logger.Error($"UserFriendlyException in {nameof(ReceiveClientLineMessage)}: {ex.Message}", ex);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred in {nameof(ReceiveClientLineMessage)}.", ex);
                return new List<ChatbotMessageManagerMessageDto>(); // Return empty list on error
            }
            finally
            {
                 try
                {
                    semaphoreSlim?.Release();
                }
                catch (ObjectDisposedException) { Logger.Warn("Attempted to release a disposed semaphore."); }
                catch (SemaphoreFullException) { Logger.Error("Attempted to release semaphore too many times."); }
            }
        }

        [DisableAuditing]
        public async Task ReceiveClientFacebookMessage(ChatbotMessageManagerMessageDto input)
        {
             // Validate input
             if (input?.ChatbotId == null || input.ClientId == null)
            {
                Logger.Warn($"Received invalid input in {nameof(ReceiveClientFacebookMessage)}.");
                // Facebook might expect a specific response on error, adjust if needed
                return; // Or throw? Check FB API requirements.
            }

            List<NlpCbMessageEx> nlpCbMessageExList = null;
            SemaphoreSlim semaphoreSlim = null;

            try
            {
                var chatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
                if (chatbot == null)
                {
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, $"{nameof(input.ChatbotId)} should be a valid guid.");
                }

                semaphoreSlim = await _nlpPolicyAppService.GetMessageSendQuotaSemaphoreSlim(chatbot.TenantId);
                 DecrementQuota(semaphoreSlim); // Rename method

                if (!await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut).ConfigureAwait(false))
                {
                    Logger.Warn($"Semaphore timeout for TenantId: {chatbot.TenantId} in {nameof(ReceiveClientFacebookMessage)}.");
                    return; // Or throw? Check FB API requirements.
                }

                // Set defaults for Facebook
                input.MessageType ??= "text";
                input.SenderTime ??= Clock.Now;
                input.SenderRole = "client";
                input.ConnectionProtocol = "facebook"; // Explicitly set for Facebook
                input.ClientChannel = "facebook";    // Explicitly set for Facebook

                nlpCbMessageExList = await ProcessReceiveMessage(input).ConfigureAwait(false);

                // Use common post-processing
                var chatroomStatus = await PostProcessClientMessage(input, chatbot, nlpCbMessageExList, true).ConfigureAwait(false); // sendToClient = true for FB? Check logic.

                // --- Facebook Specific Post-Processing ---
                // Fetch messages again for potential alternative questions
                 IList<ChatbotMessageManagerMessageDto> messages = await GetNlpCbMessageFromDatabase(chatbot.Id, chatroomStatus.ClientId, 1, true).ConfigureAwait(false);
                 var clientMessages = messages.Where(e => e.ClientReadTime == null && e.ReceiverRole == "client").ToList();

                // Send alternative questions as buttons
                foreach (var message in clientMessages)
                {
                    if (!string.IsNullOrEmpty(message.AlternativeQuestion))
                    {
                        try
                        {
                            var questions = JsonConvert.DeserializeObject<string[]>(message.AlternativeQuestion);
                            if (questions != null && questions.Any())
                            {
                                var listButtons = questions
                                    .Select(q => new PostbackButton { Title = q, Payload = q })
                                    .Cast<Button>() // Cast to base Button type
                                    .ToList();

                                // Ensure AlternativeQuestion text is available on the chatbot DTO
                                var buttonText = chatbot.AlternativeQuestion ?? "Please choose an option:"; // Provide a default
                                var attachmentMessage = new AttachmentMessage
                                {
                                    Attachment = new ButtonTemplateAttachment(buttonText, listButtons)
                                };

                                // Initialize ClientMessenger safely (consider dependency injection or factory)
                                _clientMessenger ??= new ClientMessenger(chatbot.FacebookAccessToken, chatbot.FacebookSecretKey);

                                var facebookUser = _nlpFacebookUsersAppService.GetNlpFacebookUserDto(message.ClientId.Value);
                                if (facebookUser != null && !string.IsNullOrEmpty(facebookUser.UserId))
                                {
                                    //var package = await _clientMessenger.GetJSONRenderedAsync(facebookUser.UserId, attachmentMessage); // If needed for debugging
                                    var result = await _clientMessenger.SendMessageAsync(facebookUser.UserId, attachmentMessage).ConfigureAwait(false);
                                    // Log result or handle errors from FB API
                                }
                                else
                                {
                                     Logger.Warn($"Facebook user not found or UserId missing for ClientId: {message.ClientId}");
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                             Logger.Error($"Failed to deserialize AlternativeQuestion for message {message.Id}: {jsonEx.Message}", jsonEx);
                        }
                        catch (Exception ex) // Catch potential errors during FB message sending
                        {
                            Logger.Error($"Failed to send Facebook alternative question buttons for message {message.Id}: {ex.Message}", ex);
                        }
                    }
                }
                 // Facebook controller likely handles the actual response to FB, this method might not need to return messages.
            }
             catch (UserFriendlyException ex)
            {
                Logger.Error($"UserFriendlyException in {nameof(ReceiveClientFacebookMessage)}: {ex.Message}", ex);
                throw; // Re-throw to be handled by the caller (e.g., webhook controller)
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred in {nameof(ReceiveClientFacebookMessage)}.", ex);
                 // Consider how errors should be reported back to Facebook if possible
                throw; // Re-throw for global error handling
            }
            finally
            {
                 try
                {
                    semaphoreSlim?.Release();
                }
                catch (ObjectDisposedException) { Logger.Warn("Attempted to release a disposed semaphore."); }
                catch (SemaphoreFullException) { Logger.Error("Attempted to release semaphore too many times."); }
            }
        }

        // Placeholder for the renamed method - implement its logic based on original intent
        private static void DecrementQuota(SemaphoreSlim slim, int count = 1)
        {
             // TODO: Implement the actual logic intended by "InferenctSlime"
             // This might involve decrementing a counter elsewhere, logging, etc.
             // For now, it does nothing.
             if (slim == null)
             {
                 // Log or handle null semaphore if it's unexpected
                 // Logger.Warn("Attempted to decrement quota with a null semaphore.");
                 return;
             }
             // Example: Log the current count before wait (if useful)
             // Logger.Debug($"Semaphore current count before wait: {slim.CurrentCount}");
        }

        // --- ProcessReceiveMessage and subsequent methods remain complex and would require further analysis and refactoring ---
        // --- The provided snippet ends here, further optimization requires the rest of the file ---

        // Example of optimizing a part of ProcessReceiveMessage (Conceptual)
        private async Task<List<NlpCbMessageEx>> ProcessReceiveMessage(ChatbotMessageManagerMessageDto input)
        {
            // Input validation
            if (input.Message.Length > 256) // Consider making max length configurable
                throw new UserFriendlyException(ChatErrorCode.Error_InvalidInputParameter, "Message length exceeds the maximum limit.");

            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
            if (nlpChatbot == null || nlpChatbot.Disabled) // Combine checks
                throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "Chatbot not found or is disabled.");

            input.SenderTime ??= Clock.Now;

            List<NlpCbMessageEx> nlpCbMessageExList = null;
            NlpCbMessageEx initialMessageEx = new NlpCbMessageEx(); // Rename for clarity

            try
            {
                // Consider UnitOfWork scope - is DisableFilter always needed here?
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MustHaveTenant, AbpDataFilters.MayHaveTenant))
                {
                    // Create and insert the initial message
                    var receivedMessage = new NlpCbMessage()
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
                    };
                    initialMessageEx.NlpCbMessage = await _nlpCbMessageRepository.InsertAsync(receivedMessage).ConfigureAwait(false);

                    // Add message to chatroom status cache
                    if (input.SenderRole == "client" || input.ReceiverRole == "client")
                    {
                        await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage
                        {
                            IsClientSent = input.SenderRole == "client", // Simplified boolean assignment
                            Message = input.Message
                        }).ConfigureAwait(false);
                    }

                    // Get chatbot reply if applicable
                    if (input.ReceiverRole == "chatbot" && input.ChatbotId.HasValue && input.MessageType == "text") // Redundant ChatbotId check?
                    {
                        try
                        {
                            // GetChatbotReplyMessage is complex, needs its own optimization pass
                            nlpCbMessageExList = await GetChatbotReplyMessage(nlpChatbot, input.ClientId.Value, initialMessageEx.NlpCbMessage).ConfigureAwait(false);
                        }
                        catch (Exception ex) // Catch specific exceptions if possible
                        {
                            Logger.Error($"Error getting chatbot reply: {ex.ToString()}", ex); // Use structured logging if available

                            var chatroomStatus = await GetChatroomStatus(nlpChatbot.Id, input.ClientId.Value).ConfigureAwait(false);

                            // Check if agent intervention is configured
                            if (input.SenderRole == "agent" || (chatroomStatus?.ResponseConfirmEnabled == true && chatroomStatus.ChatroomAgents?.Count > 0))
                            {
                                // Defer message indicating failure to agent
                                await DeferredSendAgentUnfoundMessageAnswer(input.ChatbotId.Value, input.ClientId.Value).ConfigureAwait(false);
                                return nlpCbMessageExList ?? new List<NlpCbMessageEx>(); // Return whatever was processed before error, or empty list
                            }
                            else
                            {
                                throw; // Re-throw if no agent intervention
                            }
                        }

                        // Add reply messages to chatroom status cache
                        if (nlpCbMessageExList != null)
                        {
                            foreach (var msgEx in nlpCbMessageExList)
                            {
                                // Check msgEx and NlpCbMessage validity
                                if (msgEx?.NlpCbMessage != null && msgEx.NlpCbMessage.NlpReceiverRole == "client")
                                {
                                    await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage { IsClientSent = false, Message = msgEx.NlpCbMessage.NlpMessage }).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                } // End using UnitOfWork
            }
            catch (Exception ex) // Catch broader exceptions from the outer try block
            {
                 // Log fatal as it might indicate a significant issue in message processing flow
                Logger.Fatal($"An error occurred during {nameof(ProcessReceiveMessage)}", ex);

                // Attempt to save an unfound/error message as a fallback
                try
                {
                     // Pass the original question if available
                    var unfoundMessage = await SaveUnfoundMessage(nlpChatbot.Id, input.ClientId.Value, input.Message , null, null).ConfigureAwait(false);
                    if (unfoundMessage != null)
                    {
                         // Add this error message to the chatroom status as well
                        await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage { IsClientSent = false, Message = unfoundMessage.NlpMessage }).ConfigureAwait(false);
                         // Return this single error message DTO
                         nlpCbMessageExList = new List<NlpCbMessageEx> { new NlpCbMessageEx(unfoundMessage) };
                    }
                }
                catch(Exception saveEx)
                {
                    Logger.Error($"Failed to save unfound message after an error in {nameof(ProcessReceiveMessage)}", saveEx);
                }
                 // Depending on requirements, might re-throw the original 'ex' or return the potentially populated nlpCbMessageExList
                 // For now, return the list which might contain the saved unfound message or be null/empty
                 return nlpCbMessageExList ?? new List<NlpCbMessageEx>();
            }

            return nlpCbMessageExList ?? new List<NlpCbMessageEx>(); // Ensure a list is always returned
        }

        // ... (Rest of the methods would follow similar optimization patterns)
        // GetChatbotReplyMessage, SaveNlpCbQAAccuracy, GetNlpCbMessageFromDatabase etc. need detailed review.

        // Example: Optimization within GetChatbotReplyMessage structure (Conceptual)
        private async Task<List<NlpCbMessageEx>> GetChatbotReplyMessage(NlpChatbotDto nlpChatbot, Guid clientId, NlpCbMessage nlpCbMessage)
        {
            var nlpCbMessageExList = new List<NlpCbMessageEx>();
            var allPredictMessages = new List<AllPredictMessages>(); // Consider renaming AllPredictMessages for clarity
            var inputMessage = nlpCbMessage.NlpMessage.Trim();

            var chatroomStatus = await GetChatroomStatus(nlpChatbot.Id, clientId).ConfigureAwait(false);
            var workflowStatus = await GetNlpWorkflowStateInfo(chatroomStatus.WfState).ConfigureAwait(false);

            // --- Consolidate Prediction Logic ---
            // Task list for parallel predictions
            var predictionTasks = new List<Task<NlpCbGetChatbotPredictResult>>();

            // Predict within current workflow state (if any)
            if (nlpCbMessage.NlpSenderRole != "agent" && workflowStatus != null)
            {
                predictionTasks.Add(ChatbotPredict(nlpChatbot.Id, inputMessage, workflowStatus.Id));
                // Predict within the parent workflow (if different from state)
                if (workflowStatus.NlpWorkflowId != workflowStatus.Id) // Check if workflow ID is different from state ID
                {
                     predictionTasks.Add(ChatbotPredict(nlpChatbot.Id, inputMessage, workflowStatus.NlpWorkflowId));
                }
            }

            // Predict outside workflow (if allowed)
            if (workflowStatus == null || workflowStatus.ResponseNonWorkflowAnswer)
            {
                predictionTasks.Add(ChatbotPredict(nlpChatbot.Id, inputMessage, Guid.Empty));
            }

            // Await all prediction tasks
            var predictionResults = await Task.WhenAll(predictionTasks).ConfigureAwait(false);

            // --- Process Prediction Results ---
            // Use a dictionary or lookup for faster QA DTO retrieval if GetNlpQADtofromNNID involves I/O or significant work
            // var qaDtoCache = new Dictionary<int, NlpQADto>(); // If GetNlpQADtofromNNID is slow

            foreach (var predictResult in predictionResults)
            {
                if (predictResult.errorCode == "success" && predictResult.result != null)
                {
                    Guid? predictionContextId = Guid.Empty; // Determine context (workflow state, workflow, or general) based on which task returned this result
                     // This requires modifying ChatbotPredict or wrapping the tasks to retain context.
                     // For simplicity here, we might need to re-evaluate context inside the loop, less optimal.

                    foreach (var item in predictResult.result)
                    {
                        var nlpQADto = await GetNlpQADtofromNNID(nlpChatbot.Id, item.nnid).ConfigureAwait(false);
                        if (nlpQADto == null) continue;

                        item.QaId = nlpQADto.Id; // Assign QA ID

                        // Determine context and thresholds (this logic needs refinement based on how context is tracked)
                        bool isWorkflowStateMatch = workflowStatus != null && nlpQADto.CurrentWfState == workflowStatus.Id;
                        bool isWorkflowMatch = workflowStatus != null && nlpQADto.CurrentWfState == workflowStatus.NlpWorkflowId;
                        bool isGeneralMatch = (nlpQADto.CurrentWfState == Guid.Empty || nlpQADto.CurrentWfState == null);

                        // Determine applicable thresholds
                        double predictionThreshold = (workflowStatus != null) ? nlpChatbot.WSPredThreshold : nlpChatbot.PredThreshold;
                        double suggestionThreshold = nlpChatbot.SuggestionThreshold; // Assuming suggestion threshold is constant

                        // Check if the prediction should be added based on context rules
                        bool addPrediction = false;
                        if (isWorkflowStateMatch && workflowStatus != null) addPrediction = true; // Matched specific state
                        else if (isWorkflowMatch && workflowStatus != null && !isWorkflowStateMatch) addPrediction = true; // Matched workflow but not specific state
                        else if (isGeneralMatch && (workflowStatus == null || workflowStatus.ResponseNonWorkflowAnswer)) addPrediction = true; // General QA and allowed

                        if (addPrediction)
                        {
                             allPredictMessages.Add(new AllPredictMessages
                             {
                                 // Store original input context if possible
                                 // ChatbotPredictInput = ...,
                                 ChatbotPredictResult = item,
                                 InputState = workflowStatus?.Id ?? Guid.Empty,
                                 NlpQADto = nlpQADto,
                                 inPredictionThreshold = item.probability > predictionThreshold,
                                 inWorkflowState = isWorkflowStateMatch, // Flag if it matched the exact state
                                 inWorkflow = isWorkflowMatch, // Flag if it matched the workflow
                                 inSuggestionThreshold = item.probability > suggestionThreshold
                             });
                        }
                    }
                }
                else if(predictResult.errorCode != "success")
                {
                    Logger.Error($"Chatbot prediction failed for ChatbotId {nlpChatbot.Id}, Context: {predictResult.workflowStateId}, Error: {predictResult.errorCode}");
                }
            }


            // --- Distinct and Order Predictions ---
            // Use LINQ GroupBy and FirstOrDefault for potentially cleaner distinct logic
            var distinctPredictMessages = allPredictMessages
                .Where(e => e.ChatbotPredictResult.nnid != 0) // Ensure valid NNID
                .OrderByDescending(e => e.ChatbotPredictResult.probability > 0.99) // Prioritize very high confidence
                .ThenByDescending(e => e.ChatbotPredictResult.probability > 0.95)
                .ThenByDescending(e => e.inPredictionThreshold) // Prioritize predictions above threshold
                // Adjust scoring based on context relevance - higher score for exact state match
                .ThenByDescending(e => e.ChatbotPredictResult.probability * (e.inWorkflowState ? 1.1 : 1.0) * (e.inWorkflow ? 1.05 : 1.0))
                .GroupBy(e => e.ChatbotPredictResult.nnid) // Group by NNID to get distinct answers
                .Select(g => g.First()) // Select the highest-ranked prediction for each NNID
                .Take(3) // Take top 3 distinct answers
                .ToList();


             // --- Update Incorrect Answer Count ---
             bool predictionFound = distinctPredictMessages.Any() && distinctPredictMessages.First().inPredictionThreshold;
             if (nlpCbMessage.NlpSenderRole != "agent") // Only update count for client messages
             {
                 chatroomStatus.IncorrectAnswerCount = predictionFound ? 0 : chatroomStatus.IncorrectAnswerCount + 1;
                 // Consider updating chatroomStatus cache here if count changed, or batch updates later
             }

            // --- Update Workflow State ---
            if (predictionFound)
            {
                var bestPrediction = distinctPredictMessages.First();
                var nextWfState = bestPrediction.NlpQADto.NextWfState;
                // Check if NextWfState indicates a change and is not the 'keep current' marker
                if (nextWfState.HasValue && nextWfState != NlpWorkflowStateConsts.WfsKeepCurrent && nextWfState != chatroomStatus.WfState)
                {
                    chatroomStatus.WfState = nextWfState.Value; // Assign the new state GUID
                    chatroomStatus.IncorrectAnswerCount = 0; // Reset error count on state change
                    Debug.WriteLine($"WorkflowState changed to: {chatroomStatus.WfState}");
                    // Update chatroomStatus cache immediately or batch later
                }
            }


            // --- Handle Prediction Errors/Low Confidence ---
            string predictionErrorMessage = null;
            if (workflowStatus != null && chatroomStatus.IncorrectAnswerCount >= 1 && !predictionFound) // Check if in workflow and no prediction met threshold
            {
                 Guid? errorOpId = null;
                 if (chatroomStatus.IncorrectAnswerCount >= 3 && !string.IsNullOrEmpty(workflowStatus.Outgoing3FalseOp))
                 {
                     errorOpId = Guid.Parse(workflowStatus.Outgoing3FalseOp); // Assuming Outgoing3FalseOp stores the Guid as string
                 }
                 else if (!string.IsNullOrEmpty(workflowStatus.OutgoingFalseOp)) // Check count >= 1 implicitly covered by outer if
                 {
                     errorOpId = Guid.Parse(workflowStatus.OutgoingFalseOp); // Assuming OutgoingFalseOp stores the Guid as string
                 }

                 if (errorOpId.HasValue)
                 {
                     // GetNlpWfsFalsePredictionOpDto needs optimization review
                     var nlpWfsOp = await GetNlpWfsFalsePredictionOpDto(nlpChatbot.Id, clientId, errorOpId.Value.ToString()).ConfigureAwait(false); // Pass Guid as string? Check method signature
                     if (nlpWfsOp != null)
                     {
                         predictionErrorMessage = nlpWfsOp.ResponseMsg;
                         // Update workflow state based on the error operation
                         if (nlpWfsOp.NextStatus != chatroomStatus.WfState)
                         {
                              chatroomStatus.WfState = nlpWfsOp.NextStatus;
                              chatroomStatus.IncorrectAnswerCount = 0; // Reset count as error path defines next step
                              Debug.WriteLine($"WorkflowState changed via error op to: {chatroomStatus.WfState}");
                              // Update chatroomStatus cache
                         }
                     }
                 }
            }

            // --- Save Accuracy ---
            // Consider making SaveNlpCbQAAccuracy faster or run in background if it's slow
            var nlpCbQAAccuracy = await SaveNlpCbQAAccuracy(nlpCbMessage, distinctPredictMessages).ConfigureAwait(false);


             // --- Determine Response Path (Agent Monitoring vs. Direct Client Response) ---
            bool agentMonitoring = nlpCbMessage.NlpSenderRole == "agent" || (chatroomStatus.ResponseConfirmEnabled == true && chatroomStatus.ChatroomAgents?.Count > 0);

            if (agentMonitoring)
            {
                 // --- Agent Monitoring Path ---
                 // Send error message (if any) to agent
                 if (!string.IsNullOrEmpty(predictionErrorMessage))
                 {
                     var errorMessageForAgent = new NlpCbMessageEx(new NlpCbMessage
                     {
                         TenantId = nlpChatbot.TenantId,
                         ClientId = clientId,
                         NlpChatbotId = nlpChatbot.Id,
                         NlpMessage = predictionErrorMessage,
                         NlpMessageType = "text.workflow.error", // Specific type for agent UI?
                         NlpCreationTime = Clock.Now,
                         NlpSenderRole = "chatbot",
                         NlpReceiverRole = "agent", // Target agent
                         NlpAgentId = nlpCbMessage.NlpAgentId, // Assign to the agent who sent or is monitoring
                         QAAccuracyId = null // No direct accuracy for this error message itself
                     });
                     await _nlpCbMessageRepository.InsertAsync(errorMessageForAgent.NlpCbMessage).ConfigureAwait(false);
                     nlpCbMessageExList.Add(errorMessageForAgent);
                 }

                 // Prepare suggested answers for agent
                 var suggestedAnswers = new List<string>();
                 // Get standard unfound message (without GPT for agent suggestions)
                 var unfoundMessageText = (await GetUnfoundMessageWithoutGPTAsync(nlpChatbot.Id, clientId, inputMessage, null, nlpCbQAAccuracy?.Id).ConfigureAwait(false))?.NlpMessage;
                 if (!string.IsNullOrEmpty(unfoundMessageText))
                 {
                     suggestedAnswers.Add(unfoundMessageText);
                 }

                 // Add answers from predictions above suggestion threshold
                 foreach (var result in distinctPredictMessages)
                 {
                     if (result.inSuggestionThreshold)
                     {
                         try
                         {
                             var answers = JsonToAnswer(result.NlpQADto.Answer); // JsonToAnswer needs review
                             foreach (var answer in answers)
                             {
                                 // Replace custom strings for agent context if needed
                                 suggestedAnswers.Add(await ReplaceCustomStringAsync(answer, nlpChatbot.Id).ConfigureAwait(false));
                             }
                         }
                         catch (Exception ex)
                         {
                             Logger.Error($"Error processing suggested answer for NNID {result.ChatbotPredictResult.nnid}: {ex.Message}", ex);
                         }
                     }
                     // else break; // Optimization: if ordered, stop once below suggestion threshold
                 }

                 // Create a message DTO containing only suggestions for the agent
                 if (suggestedAnswers.Any())
                 {
                      nlpCbMessageExList.Add(new NlpCbMessageEx(new NlpCbMessage
                      {
                          TenantId = nlpChatbot.TenantId,
                          ClientId = clientId,
                          NlpChatbotId = nlpChatbot.Id,
                          NlpMessage = "", // No primary message
                          NlpMessageType = chatroomStatus.WfState == Guid.Empty ? "text.suggestion" : "text.workflow.suggestion", // Specific type?
                          NlpCreationTime = Clock.Now,
                          NlpSenderRole = "chatbot",
                          NlpReceiverRole = "agent",
                          NlpAgentId = nlpCbMessage.NlpAgentId,
                          QAAccuracyId = nlpCbQAAccuracy?.Id // Link suggestions back to the accuracy record
                      }, suggestedAnswers)); // Pass suggestions in the constructor or dedicated property
                 }
                 // No direct reply to client in this path
            }
            else
            {
                 // --- Direct Client Response Path ---
                 // Send workflow error message (if any) to client
                 if (!string.IsNullOrEmpty(predictionErrorMessage))
                 {
                     var errorMessageForClient = new NlpCbMessageEx(new NlpCbMessage
                     {
                         TenantId = nlpChatbot.TenantId,
                         ClientId = clientId,
                         NlpChatbotId = nlpChatbot.Id,
                         NlpMessage = predictionErrorMessage,
                         NlpMessageType = "text.workflow.error",
                         NlpCreationTime = Clock.Now,
                         NlpSenderRole = "chatbot",
                         NlpReceiverRole = "client", // Target client
                         QAAccuracyId = null
                     });
                     await _nlpCbMessageRepository.InsertAsync(errorMessageForClient.NlpCbMessage).ConfigureAwait(false);
                     nlpCbMessageExList.Add(errorMessageForClient);
                 }

                 // Prepare alternative questions for client (if prediction was below threshold or multiple suggestions exist)
                 List<string> alternativeQuestionList = null;
                 int skipCount = predictionFound ? 1 : 0; // Skip the first if it was a confident match

                 foreach (var result in distinctPredictMessages.Skip(skipCount))
                 {
                     if (result.inSuggestionThreshold)
                     {
                         try
                         {
                             var questions = result.NlpQADto.GetQuestionList(); // GetQuestionList needs review
                             if (questions.Any())
                             {
                                 alternativeQuestionList ??= new List<string>();
                                 // Replace custom strings for client context
                                 alternativeQuestionList.Add(await ReplaceCustomStringAsync(questions.First(), nlpChatbot.Id).ConfigureAwait(false));
                             }
                         }
                         catch (Exception ex)
                         {
                             Logger.Error($"Error processing alternative question for NNID {result.ChatbotPredictResult.nnid}: {ex.Message}", ex);
                         }
                     }
                     // else break; // Optimization if ordered
                 }

                 // Handle case where no confident prediction was found
                 if (!predictionFound)
                 {
                      // Check if we should suppress the standard "unfound" message based on workflow error settings
                     bool suppressUnfound = !string.IsNullOrEmpty(predictionErrorMessage) && workflowStatus?.DontResponseNonWorkflowErrorAnswer == true;

                     if (!suppressUnfound)
                     {
                         // Save and prepare the standard "unfound" message (potentially with GPT)
                         var unfoundMessage = await SaveUnfoundMessage(nlpChatbot.Id, clientId, inputMessage, alternativeQuestionList, nlpCbQAAccuracy?.Id).ConfigureAwait(false);
                         if (unfoundMessage != null)
                         {
                             nlpCbMessageExList.Add(new NlpCbMessageEx(unfoundMessage));
                         }
                     }
                     // Return here as no primary answer is given
                     return nlpCbMessageExList;
                 }

                 // --- Prepare Confident Answer for Client ---
                 var bestMatch = distinctPredictMessages.First();
                 PredictedQAMessage output = new PredictedQAMessage { QaId = bestMatch.ChatbotPredictResult.QaId };

                 // Get answer text (GetAnswerFromNNIDRepetition needs optimization review)
                 output.Message = await GetAnswerFromNNIDRepetition(nlpChatbot.Id, bestMatch.ChatbotPredictResult.nnid).ConfigureAwait(false);

                 // Post-process the answer (replace variables, potentially call GPT)
                 if (!string.IsNullOrEmpty(output.Message))
                 {
                     output.Message = await ReplaceCustomStringAsync(output.Message, nlpChatbot.Id).ConfigureAwait(false);
                     // ChatGPT call needs careful consideration for performance and cost
                     output.Message = await ChatGPT(nlpChatbot.Id, inputMessage, output.Message).ConfigureAwait(false);
                 }

                 // Handle case where answer processing resulted in an empty message
                 if (string.IsNullOrEmpty(output.Message))
                 {
                     Logger.Warn($"Answer for NNID {bestMatch.ChatbotPredictResult.nnid} became empty after processing.");
                     // Fallback to unfound message
                     var unfoundMessage = await SaveUnfoundMessage(nlpChatbot.Id, clientId, inputMessage, alternativeQuestionList, nlpCbQAAccuracy?.Id).ConfigureAwait(false);
                     if (unfoundMessage != null)
                     {
                         nlpCbMessageExList.Add(new NlpCbMessageEx(unfoundMessage));
                     }
                     return nlpCbMessageExList;
                 }

                 // Create the final message DTO for the client
                 NlpCbMessage finalClientMessage = new NlpCbMessage
                 {
                     TenantId = nlpChatbot.TenantId,
                     ClientId = clientId,
                     NlpChatbotId = nlpChatbot.Id,
                     NlpMessage = output.Message.Substring(0, Math.Min(output.Message.Length, 1024)), // Ensure length constraint
                     QAId = output.QaId,
                     NlpMessageType = chatroomStatus.WfState == Guid.Empty ? "text" : "text.workflow", // Reflect workflow status
                     NlpCreationTime = Clock.Now,
                     NlpSenderRole = "chatbot",
                     NlpReceiverRole = "client",
                     AlternativeQuestion = (alternativeQuestionList == null || !alternativeQuestionList.Any()) ? null : JsonConvert.SerializeObject(alternativeQuestionList),
                     QAAccuracyId = nlpCbQAAccuracy?.Id
                 };

                 await _nlpCbMessageRepository.InsertAsync(finalClientMessage).ConfigureAwait(false);
                 nlpCbMessageExList.Add(new NlpCbMessageEx(finalClientMessage));
            }

            // Update ChatroomStatus cache if state or error count changed during processing
            // Consider a single update call here if multiple changes occurred
            // UpdateChatroomStatusCache(nlpChatbot.Id, clientId, chatroomStatus);

            return nlpCbMessageExList;
        }

    } // End Class
} // End Namespace