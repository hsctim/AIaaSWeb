using System;
using System.IO;
using System.Threading.Tasks;
using Abp;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.AspNetZeroCore.Net;
using Abp.Auditing;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Runtime.Session;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization.Users;
using AIaaS.Authorization.Users.Profile;
using AIaaS.Authorization.Users.Profile.Dto;
using AIaaS.Friendships;
using AIaaS.Storage;
using Abp.Authorization;
using AIaaS.Nlp;
using Abp.Application.Services;
using AIaaS.Nlp.Lib;
using AIaaS.Helpers;
using System.Collections.Generic;
using System.Globalization;
using Abp.Web.Models;
using AIaaS.Chatbot;
using Abp.Runtime.Caching;
using ApiProtectorDotNet;
using Abp.UI;
using AIaaS.Web.Chatbot;
using AIaaS.Web.Security;
using System.Net;
using AIaaS.Web.Models.Line;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using isRock.LineBot;
using System.Linq;
using System.Text.RegularExpressions;

namespace AIaaS.Web.Controllers
{

    [AbpAllowAnonymous]
    [Route("[controller]")]
    public class LineController : AIaaSControllerBase
    {
        //private static readonly Guid _defaultBotPictureId = new Guid("00000000-0000-0000-0000-000000000000");

        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly INlpLineUsersAppService _nlpLineUsersAppService;
        private readonly ICacheManager _cacheManager;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly IChatbotMessageManager _chatbotMessageManager;
        private readonly AntiDDoS _antiDDoS;
        //private readonly string _replyApiUrl = "https://api.line.me/v2/bot/message/reply";


        public LineController(
            INlpChatbotsAppService nlpChatbotsAppService,
            INlpLineUsersAppService nlpLineUsersAppService,
            ICacheManager cacheManager,
            NlpChatbotFunction nlpChatbotFunction,
            IChatbotMessageManager chatbotMessageManager,
            AntiDDoS antiDDoS)
        {
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _nlpLineUsersAppService = nlpLineUsersAppService;
            _cacheManager = cacheManager;
            _nlpChatbotFunction = nlpChatbotFunction;
            _chatbotMessageManager = chatbotMessageManager;
            _antiDDoS = antiDDoS;
        }

        [AbpAllowAnonymous]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 20, TimeWindowSeconds: 60)]
        //[HttpPost]
        [HttpPost("{id?}")]
        public async Task<IActionResult> Webhook([FromRoute] Guid id, [FromBody] ReceivedMessage request)
        {
            try
            {
                if (id == Guid.Empty)
                    return NotFound();

                if (_antiDDoS.isLimited(id.ToString(), "LineWebhook", 10, 100))
                {
                    return Problem(statusCode: StatusCodes.Status429TooManyRequests);
                }

                var chatbot = _nlpChatbotFunction.GetChatbotDto(id);
                if (chatbot == null || chatbot.Disabled == true || chatbot.IsDeleted == true || chatbot.EnableLine == false)
                    return NotFound();

                var bot = new isRock.LineBot.Bot(chatbot.LineToken);

                foreach (var lineEvent in request.events)
                {
                    try
                    {
                        List<isRock.LineBot.MessageBase> replyMessages = null;
                        List<isRock.LineBot.TemplateActionBase> pushMessageActions = null;
                        string alternativeQuestions = "";
                        String lineAPIResult = null;

                        if (lineEvent.type.ToLower() == "message" && lineEvent.message.type.ToLower() == "text")
                        {
                            var lineUser = _nlpLineUsersAppService.GetNlpLineUserDto(lineEvent.source.userId, chatbot.LineToken);

                            var input = new ChatbotMessageManagerMessageDto()
                            {
                                ReceiverRole = "chatbot",
                                ClientId = lineUser.Id,
                                ChatbotId = chatbot.Id,
                                MessageType = "text",
                                ConnectionProtocol = "line",
                                ClientChannel = "line",
                                Message = lineEvent.message.text,
                                SenderImage = lineUser.PictureUrl,
                                SenderName = lineUser.UserName
                            };

                            var chatbotMessageManagerMessageDtoList = await _chatbotMessageManager.ReceiveClientLineMessage(input);

                            foreach (var message in chatbotMessageManagerMessageDtoList)
                            {
                                replyMessages ??= new List<isRock.LineBot.MessageBase>();
                                replyMessages.Add(new isRock.LineBot.TextMessage(StripHTML(message.Message)));

                                if (message.AlternativeQuestion.IsNullOrEmpty() == false)
                                {
                                    var questions = JsonConvert.DeserializeObject<string[]>(message.AlternativeQuestion);

                                    foreach (var question in questions)
                                    {
                                        pushMessageActions ??= new List<isRock.LineBot.TemplateActionBase>();
                                        pushMessageActions.Add(new isRock.LineBot.MessageAction()
                                        {
                                            label = StripHTML(question),
                                            text = StripHTML(question)
                                        });

                                        alternativeQuestions = alternativeQuestions + "    [ " + StripHTML(question) + " ]";
                                    }
                                }
                            }

                            if (replyMessages != null && replyMessages.Count > 0)
                            {
                                if (replyMessages.Count > 5)
                                    replyMessages = replyMessages.TakeLast(5).ToList();

                                lineAPIResult = bot.ReplyMessage(request.events.FirstOrDefault()?.replyToken, replyMessages);

                                await _chatbotMessageManager.OnClientSendReceipt(chatbot.Id, lineUser.Id);
                            }

                            if (pushMessageActions != null && pushMessageActions.Count > 0)
                            {
                                var ButtonTemplate = new isRock.LineBot.ButtonsTemplate()
                                {
                                    altText = chatbot.AlternativeQuestion + alternativeQuestions, // + "\n" + chatbot.AlternativeQuestion,//"替代文字(在無法顯示Button Template的時候顯示)",
                                    text = chatbot.AlternativeQuestion,
                                    actions = pushMessageActions //設定回覆動作
                                };

                                lineAPIResult = bot.PushMessage(request.events.FirstOrDefault()?.source.userId, ButtonTemplate);

                                await _chatbotMessageManager.OnClientSendReceipt(chatbot.Id, lineUser.Id);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.ToString(), ex);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), ex);
                return NotFound();
            }
        }

        //private Models.Line.TextMessage BuildTextMessage(string msg)
        //{
        //    return new Models.Line.TextMessage()
        //    {
        //        text = msg
        //    };
        //}


        private string StripHTML(string input)
        {
            if (input.IsNullOrEmpty())
                return input;

            input = input.Replace("<br>", "\n").Replace("<BR>", "\n");
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

    }
}