using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Identity;
using ApiProtectorDotNet;
using Abp.Localization;
using System.Linq;
using System;
using AIaaS.Configuration;
using Microsoft.Extensions.Configuration;
using AIaaS.Nlp;
using IdentityServer4.Extensions;

namespace AIaaS.Web.Controllers
{
    public class WebController : AIaaSControllerBase
    {
        private readonly NlpChatbotFunction _nlpChatbotFunction;

        public WebController(NlpChatbotFunction nlpChatbotFunction
            )

        {
            _nlpChatbotFunction = nlpChatbotFunction;
        }


        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        [Route("/WebChat/{chatbotId?}/{question?}")]
        public IActionResult Index(Guid chatbotId, string question)
        {
            if (chatbotId == Guid.Empty)
                return NotFound();

            var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);

            if (chatbot == null || chatbot.EnableWebChat == false)
                return NotFound();

            string url = "/webchat/index.html?chatbotId=" + chatbotId.ToString();

            if (question.IsNullOrEmpty() == false)
                url += "&question=" + Uri.EscapeDataString(question);

            return Redirect(url);
        }
    }
}