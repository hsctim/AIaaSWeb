using Abp.Application.Services;
using Abp.Auditing;
using Abp.Timing;
using Abp.UI;
using AIaaS.Chatbot;
using AIaaS.Nlp.Dtos;
using AIaaS.Nlp;
using AIaaS.Web.Chatbot;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Abp.Extensions;
using Abp.EntityFrameworkCore.EFPlus;
using AIaaS.Web.Chatbot.Dto;
using System.Linq;
using System.Threading;
using ReflectSoftware.Facebook.Messenger.Common.Models;
using ReflectSoftware.Facebook.Messenger.Client;
using ReflectSoftware.Facebook.Messenger.Common.Models.Client;

namespace AIaaS.Web.Chatbot
{
    public partial class ChatbotMessageManager : ApplicationService, IChatbotMessageManager
    {
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


    }
}
