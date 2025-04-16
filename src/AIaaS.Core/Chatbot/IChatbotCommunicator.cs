using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using Abp.RealTime;
using AIaaS.Friendships;
using AIaaS.Nlp.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace AIaaS.Chatbot
{
    public interface IChatbotCommunicator
    {
        //void SendMessageToClients(IReadOnlyList<IOnlineClient> clients, IList<ChatbotMessageManagerMessageDto> messages);


        //void SendMessagesToClient(IClientProxy client, string messageName, IList<object> messages);
        void SendMessageToClient(IClientProxy client, string messageName, object message);

        void SendMessageToClient(IOnlineClient client, string messageName, object message);



        //void SendMessageToClient(string connectionId, string messageName, object message);

        void SendMessageToClients(IReadOnlyList<IOnlineClient> clients, string messageName, object message);
        //void SendMessagesToTenant(int tenantId, string messageName, IList<object> messages);

        void SendMessageToTenant(int tenantId, string messageName, object message);

        //void SendMessage_UpdateNlpChatroomStatus(int tenantId, NlpChatroomStatus chatroomStatus);
    }
}
