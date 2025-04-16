using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.RealTime;
using Abp.Runtime.Caching;
using Abp.Threading;
using AIaaS.Helpers;
using AIaaS.Nlp;
using System;

namespace AIaaS.Chatbot
{
    public class ChatbotStateWatcher : ISingletonDependency
    {
        private readonly IOnlineClientManager<ChatbotChannel> _onlineClientManager;
        private readonly ICacheManager _cacheManager;
        private readonly IChatbotMessageManager _chatbotMessageManager;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public ChatbotStateWatcher(
            IOnlineClientManager<ChatbotChannel> onlineClientManager,
            ICacheManager cacheManager,
        IChatbotMessageManager chatbotMessageManager,
        IUnitOfWorkManager unitOfWorkManager)
        {
            _onlineClientManager = onlineClientManager;
            _cacheManager = cacheManager;
            _chatbotMessageManager = chatbotMessageManager;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public void Initialize()
        {
            _onlineClientManager.ClientDisconnected += OnlineClientManager_ClientDisconnected; ;
        }

        private void OnlineClientManager_ClientDisconnected(object sender, OnlineClientEventArgs e)
        {
            _unitOfWorkManager.WithUnitOfWork( () =>
            {
                AsyncHelper.RunSync(() => _chatbotMessageManager.DisconnectNotification(e.Client.ConnectionId.ToString()));
            });
        }
    }
}