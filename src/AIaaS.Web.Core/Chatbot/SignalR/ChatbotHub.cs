using System;
using System.Threading.Tasks;
using Abp;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Extensions;
using Abp.Localization;
using Abp.RealTime;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Castle.Core.Logging;
using AIaaS.Chatbot;
using AIaaS.Nlp;
using AIaaS.Web.Security;
using Abp.Domain.Uow;

namespace AIaaS.Web.Chatbot.SignalR
{
    public class ChatbotHub : OnlineClientHubBase
    {
        private readonly IChatbotMessageManager _chatbotMessageManager;
        private readonly ILocalizationManager _localizationManager;
        IOnlineClientManager<ChatbotChannel> _onlineClientManager;
        private readonly AntiDDoS _antiDDoS;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private bool _isCallByRelease;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatbotHub"/> class.
        /// </summary>
        /// 
        public ChatbotHub(
            IChatbotMessageManager chatbotMessageManager,
            ILocalizationManager localizationManager,
            AntiDDoS antiDDoS,
            IOnlineClientManager<ChatbotChannel> onlineClientManager,
            IOnlineClientInfoProvider clientInfoProvider,
            NlpChatbotFunction nlpChatbotFunction,
            IUnitOfWorkManager unitOfWorkManager
            ) : base(onlineClientManager, clientInfoProvider)
        {
            _chatbotMessageManager = chatbotMessageManager;
            _localizationManager = localizationManager;
            _onlineClientManager = onlineClientManager;
            _antiDDoS = antiDDoS;
            _nlpChatbotFunction = nlpChatbotFunction;
            _unitOfWorkManager = unitOfWorkManager;
            Logger = NullLogger.Instance;
        }


        /// <summary>
        /// Client送來的Message
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<string> SendMessage(SendChatbotMessageInput input)
        {
            ///AntiDDoS
            if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "SendMessage", 60, 60))
                return "HTTP/429";

            return await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
            {
                try
                {
                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    if (_nlpChatbotFunction.IsSignalREnabled(input.ChatbotId.Value) == false)
                        return "ServiceNotAvailable";

                    input.MessageType ??= "text";
                    input.SenderTime ??= Clock.Now;
                    input.ReceiverRole = "chatbot";
                    input.SenderRole = "client";

                    ChatbotMessageManagerMessageDto dto = Mapping(input);
                    dto.ConnectionId = Context.ConnectionId;
                    dto.ConnectionProtocol = "signal-r";
                    dto.ClientIP = _onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId)?.IpAddress;
                    dto.ClientChannel ??= "web";

                    await _chatbotMessageManager.ReceiveClientSignalRMessage(dto);

                    return "OK";
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.ToString(), ex);
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                }
            });
        }

        public async Task<string> RequestHistoryMessages(SendChatbotMessageInput input)
        {
            if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "RequestHistoryMessages", 60, 60))
                return "HTTP/429";

            return await _unitOfWorkManager.WithUnitOfWorkAsync(async() =>
            {
                try
                {
                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    if (_nlpChatbotFunction.IsSignalREnabled(input.ChatbotId.Value) == false)
                        return "ServiceNotAvailable";

                    input.ReceiverRole = "chatbot";
                    input.SenderRole = "client";

                    var message = Mapping(input);
                    message.ConnectionId = Context.ConnectionId;
                    message.ClientIP = _onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId)?.IpAddress;
                    message.ClientChannel ??= "web";
                    message.ConnectionProtocol = "signal-r";

                    await _chatbotMessageManager.SendClientHistoryMessages(Clients.Caller, message);
                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                    //throw;
                }
            });
        }


        public async Task<string> RequestGreetingMessage(SendChatbotMessageInput input)
        {
            ///AntiDDoS
            if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "RequestGreetingMessage", 60, 60))
                return "HTTP/429";

            return await _unitOfWorkManager.WithUnitOfWorkAsync (async() =>
            {
                try
                {
                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    if (_nlpChatbotFunction.IsSignalREnabled(input.ChatbotId.Value) == false)
                        return "ServiceNotAvailable";

                    input.SenderRole = "client";
                    input.ReceiverRole = "chatbot";

                    var message = Mapping(input);
                    message.ConnectionId = Context.ConnectionId;
                    message.ClientIP = _onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId)?.IpAddress;
                    message.ClientChannel ??= "web";
                    message.ConnectionProtocol = "signal-r";

                    await _chatbotMessageManager.SendClientGreetingMessage(Clients.Caller, message);
                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                    //throw;
                }
            });
        }

        public async Task<string> SendReceipt(SendChatbotMessageInput input)
        {
            ///AntiDDoS
            if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "SendReceipt", 60, 60))
                return "HTTP/429";

            return await _unitOfWorkManager.WithUnitOfWorkAsync(async() =>
            {
                try
                {
                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    if (_nlpChatbotFunction.IsSignalREnabled(input.ChatbotId.Value) == false)
                        return "ServiceNotAvailable";

                    input.SenderRole = "client";
                    input.ReceiverRole = "chatbot";

                    var message = Mapping(input);
                    message.ConnectionId = Context.ConnectionId;
                    message.ClientIP = _onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId)?.IpAddress;
                    message.ClientChannel ??= "web";
                    message.ConnectionProtocol = "signal-r";

                    await _chatbotMessageManager.ReceiveClientReceipt(message);
                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                }
            });
        }

        public async Task<string> ClientReconnect(SendChatbotMessageInput input)
        {
            ///AntiDDoS
            if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "ClientReconnect", 60, 60))
                return "HTTP/429";

            return await _unitOfWorkManager.WithUnitOfWorkAsync(async() =>
            {
                try
                {
                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    if (_nlpChatbotFunction.IsSignalREnabled(input.ChatbotId.Value) == false)
                        return "ServiceNotAvailable";

                    input.SenderRole = "client";
                    input.ReceiverRole = "chatbot";

                    var message = Mapping(input);
                    message.ConnectionId = Context.ConnectionId;
                    message.ClientIP = _onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId)?.IpAddress;
                    message.ClientChannel ??= "web";
                    message.ConnectionProtocol = "signal-r";

                    await _chatbotMessageManager.ClientReconnect(message);
                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                }
            });
        }

        public async Task<string> AgentReconnect(SendChatbotMessageInput input)
        {
            ///AntiDDoS
            if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "AgentReconnect", 60, 60))
                return "HTTP/429";

            return await _unitOfWorkManager.WithUnitOfWorkAsync( async() =>
            {
                try
                {
                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    input.AgentId = Context.GetUserId();
                    input.AgentTenantId = Context.GetTenantId();
                    if (input.AgentId.HasValue == false || input.AgentTenantId.HasValue == false)
                        return "Invalid AgentId";

                    if (input.MessageType.IsNullOrEmpty())
                        input.MessageType = "text";
                    input.SenderTime ??= Clock.Now;

                    input.SenderRole = "agent";
                    input.ReceiverRole = "chatbot";

                    ChatbotMessageManagerMessageDto dto = Mapping(input);
                    dto.ConnectionProtocol = "signal-r";
                    dto.ConnectionId = Context.ConnectionId;

                    await _chatbotMessageManager.AgentReconnect(dto);

                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                }
            });
        }

        /// <summary>
        /// Agent送來的Message
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<string> AgentSendMessage(SendChatbotMessageInput input)
        {
            ///AntiDDoS
            if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "AgentSendMessage", 60, 60))
                return "HTTP/429";

            return await _unitOfWorkManager.WithUnitOfWorkAsync(async() =>
            {
                try
                {
                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    if (input.ReceiverRole != "chatbot" && input.ReceiverRole != "client")
                        return "Invalid ReceiverRole";

                    input.AgentId = Context.GetUserId();
                    input.AgentTenantId = Context.GetTenantId();
                    if (input.AgentId.HasValue == false || input.AgentTenantId.HasValue == false)
                        return "Invalid AgentId";

                    input.MessageType ??= "text";
                    input.SenderTime ??= Clock.Now;
                    input.SenderRole = "agent";

                    ChatbotMessageManagerMessageDto dto = Mapping(input);
                    dto.ConnectionProtocol = "signal-r";
                    dto.ConnectionId = Context.ConnectionId;

                    await _chatbotMessageManager.ReceiveAgentMessage(dto);

                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn("Could not send chat message to user: " + (input.AgentId.HasValue ? input.AgentId.Value.ToString() : ""));
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                    //throw;
                }
            });
        }

        /// <summary>
        /// Agent送來的Message
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        //public string AgentSendMessageToChatbot(SendChatbotMessageInput input)
        //{
        //    try
        //    {
        //        ///AntiDDoS
        //        if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "SendMessage", 60, 20))
        //            return "HTTP/429";

        //        if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
        //            return "InvalidChatbot";

        //        input.AgentId = Context.GetUserId();
        //        input.AgentTenantId = Context.GetTenantId();
        //        if (input.AgentId.HasValue == false || input.AgentTenantId.HasValue == false)
        //            return "Invalid AgentId";

        //        input.MessageType ??= "text";
        //        input.SenderTime ??= DateTime.Now;
        //        input.SenderRole = "agent";
        //        input.ReceiverRole = "chatbot";

        //        ChatbotMessageManagerMessageDto dto = Mapping(input);
        //        dto.ConnectionProtocol = "signal-r";
        //        dto.ConnectionId = Context.ConnectionId;

        //        //_chatbotMessageManager.ReceiveAgentMessageToChatbot(dto);
        //        _chatbotMessageManager.ReceiveAgentMessage(dto);

        //        return "OK";
        //    }
        //    catch (Exception ex)
        //    {
        //        _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
        //        Logger.Warn("Could not send chat message to user: " + (input.AgentId.HasValue ? input.AgentId.Value.ToString() : ""));
        //        Logger.Warn(ex.ToString(), ex);
        //        return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
        //        //throw;
        //    }
        //}


        public async Task<string> AgentRequestHistoryMessages(SendChatbotMessageInput input)
        {
            return await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
            {
                try
                {
                    input.AgentId = Context.GetUserId();
                    input.AgentTenantId = Context.GetTenantId();
                    if (input.AgentId.HasValue == false || input.AgentTenantId.HasValue == false)
                        return "Invalid AgentId";

                    ///AntiDDoS
                    if (_antiDDoS.isLimited(input.AgentId.ToString(), "AgentRequestHistoryMessages", 60, 60))
                        return "HTTP/429";

                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    input.SenderRole = "agent";
                    input.ReceiverRole = "chatbot";

                    ChatbotMessageManagerMessageDto dto = Mapping(input);
                    dto.ConnectionProtocol = "signal-r";
                    dto.ConnectionId = Context.ConnectionId;

                    await _chatbotMessageManager.AgentRequestHistoryMessages(dto);
                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                    //throw;
                }
            });
        }


        public async Task<string> AgentSendReceipt(SendChatbotMessageInput input)
        {
            return await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
            {
                try
                {
                    input.AgentId = Context.GetUserId();
                    input.AgentTenantId = Context.GetTenantId();
                    if (input.AgentId.HasValue == false || input.AgentTenantId.HasValue == false)
                        return "Invalid AgentId";

                    ///AntiDDoS
                    if (_antiDDoS.isLimited(input.AgentId.ToString(), "AgentSendReceipt", 60, 60))
                        return "HTTP/429";

                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    input.SenderRole = "agent";
                    input.ReceiverRole = "chatbot";

                    ChatbotMessageManagerMessageDto dto = Mapping(input);
                    dto.ConnectionProtocol = "signal-r";
                    dto.ConnectionId = Context.ConnectionId;

                    //dto.ClientIP = _onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId)?.IpAddress;

                    await _chatbotMessageManager.OnAgentSendReceipt(dto);
                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn("Could not send chat message to user: " + (input.AgentId.HasValue ? input.AgentId.Value.ToString() : ""));
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                }
            });
        }

        public async Task<string> EnableResponseConfirm(SendChatbotMessageInput input)
        {
            ///AntiDDoS
            if (_antiDDoS.isLimited(_onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId).IpAddress, "EnableResponseConfirm", 60, 60))
                return "HTTP/429";

            return await _unitOfWorkManager.WithUnitOfWorkAsync(async() =>
            {
                try
                {
                    if (input.ChatbotId == null || _chatbotMessageManager.IsValidChatroom(input.ChatbotId.Value) == false)
                        return "InvalidChatbot";

                    input.AgentId = Context.GetUserId();
                    input.AgentTenantId = Context.GetTenantId();
                    if (input.AgentId.HasValue == false || input.AgentTenantId.HasValue == false)
                        return "Invalid AgentId";

                    input.SenderRole = "agent";
                    input.ReceiverRole = "chatbot";

                    ChatbotMessageManagerMessageDto dto = Mapping(input);
                    dto.ConnectionProtocol = "signal-r";
                    dto.ConnectionId = Context.ConnectionId;

                    //dto.ClientIP = _onlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId)?.IpAddress;

                    await _chatbotMessageManager.AgentEnableResponseConfirm(dto, input.EnableResponseConfirm.Value);
                    return "OK";
                }
                catch (Exception ex)
                {
                    _chatbotMessageManager.SendErrorMessage(Clients.Caller, ex.ToString());
                    Logger.Warn("Could not send chat message to user: " + (input.AgentId.HasValue ? input.AgentId.Value.ToString() : ""));
                    Logger.Warn(ex.ToString(), ex);
                    return _localizationManager.GetSource("AbpWeb").GetString("InternalServerError");
                }
            });
        }

        public void Register()
        {
            Logger.Debug("A client is registered: " + Context.ConnectionId);
        }

        protected override void Dispose(bool disposing)
        {
            if (_isCallByRelease)
            {
                return;
            }
            base.Dispose(disposing);
            if (disposing)
            {
                _isCallByRelease = true;
                //_windsorContainer.Release(this);
            }
        }

        protected ChatbotMessageManagerMessageDto Mapping(SendChatbotMessageInput input)
        {
            return new ChatbotMessageManagerMessageDto()
            {
                ClientId = input.ClientId,
                ChatbotId = input.ChatbotId,
                Message = input.Message,
                MessageType = input.MessageType,
                ReceiverRole = input.ReceiverRole,
                SenderImage = input.SenderImage,
                SenderName = input.SenderName,
                SenderRole = input.SenderRole,
                SenderTime = input.SenderTime,
                AgentId = input.AgentId,
                AgentTenantId = input.AgentTenantId,
                //UserId = input.UserId,
                //UserTenantId = input.UserTenantId
            };
        }
    }
}
