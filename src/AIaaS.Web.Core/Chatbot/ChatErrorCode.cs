using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIaaS.Web.Chatbot
{
    public static class ChatErrorCode
    {
        public const int Error_Success = 0;
        public const int Error_InvalidChatbotId = 1000;
        public const int Error_InvalidClientId = 1001;
        public const int Error_InvalidInputParameter = 1002;

        public const int Error_NoMessage = 1100;
    }


}
