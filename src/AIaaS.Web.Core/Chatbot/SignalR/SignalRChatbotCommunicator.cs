using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.ObjectMapping;
using Abp.RealTime;
using Castle.Core.Logging;
using Microsoft.AspNetCore.SignalR;
using AIaaS.Chat;
using AIaaS.Chatbot;
using AIaaS.Nlp.Dtos;
using System.Linq;
using Newtonsoft.Json;

namespace AIaaS.Web.Chatbot.SignalR
{
    public class SignalRChatbotCommunicator : IChatbotCommunicator, ITransientDependency
    {
        /// <summary>
        /// Reference to the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        //private readonly IObjectMapper _objectMapper;
        IOnlineClientManager<ChatbotChannel> _onlineClientManager;
        private readonly IHubContext<ChatbotHub> _chatbotHub;

        public SignalRChatbotCommunicator(
            //IObjectMapper objectMapper,
            IOnlineClientManager<ChatbotChannel> onlineClientManager,
            IHubContext<ChatbotHub> chatbotHub)
        {
            //_objectMapper = objectMapper;
            _onlineClientManager = onlineClientManager;
            _chatbotHub = chatbotHub;
            Logger = NullLogger.Instance;
        }


        public void SendMessageToClient(IClientProxy client, string messageName, object message)
        {
            if (client != null)
                _ = client.SendAsync(messageName, message);
        }

        public void SendMessageToClient(IOnlineClient client, string messageName, object message)
        {
            var signalRClient = GetSignalRClientOrNull(client);
            if (signalRClient != null)
                _ = signalRClient.SendAsync(messageName, message);
        }


        public void SendMessageToClients(IReadOnlyList<IOnlineClient> clients, string messageName, object message)
        {
            foreach (var client in clients)
            {
                var signalRClient = GetSignalRClientOrNull(client);
                if (signalRClient != null)
                    _ = signalRClient.SendAsync(messageName, message);
            }
        }


        public void SendMessageToTenant(int tenantId, string messageName, object message)
        {
            var clients = _onlineClientManager.GetAllClients().Where(e => e.TenantId == tenantId);
            foreach (var client in clients)
            {
                var signalRClient = GetSignalRClientOrNull(client);
                if (signalRClient != null)
                    _ = signalRClient.SendAsync(messageName, message);
            }
        }

        private IClientProxy GetSignalRClientOrNull(IOnlineClient client)
        {
            var signalRClient = _chatbotHub.Clients.Client(client.ConnectionId);

            if (signalRClient == null)
            {
                Logger.Debug("Can not get chatbot user " + client.UserId + " from SignalR hub!");
                return null;
            }

            return signalRClient;
        }
    }
}