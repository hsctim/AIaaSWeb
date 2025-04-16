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
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using ReflectSoftware.Facebook.Messenger.AspNetCore.Webhook;
using ReflectSoftware.Facebook.Messenger.Client;
using ReflectSoftware.Facebook.Messenger.Common.Models.Client;
using AIaaS.Nlp.Dtos;
using ReflectSoftware.Facebook.Messenger.Common.Models.Webhooks;

namespace AIaaS.Web.Controllers
{
    //endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
    //endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

    [AbpAllowAnonymous]
    //[Route("controller=Home}/{action=Index}/{id2}")]
    //[Route("api/[controller]")]
    [Route("[controller]")]
    public class FacebookController : AIaaSControllerBase
    {
        //private static readonly Guid _defaultBotPictureId = new Guid("00000000-0000-0000-0000-000000000000");

        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly ICacheManager _cacheManager;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly IChatbotMessageManager _chatbotMessageManager;
        private readonly AntiDDoS _antiDDoS;
        private readonly INlpFacebookUsersAppService _nlpFacebookUsersAppService;
        private MessengerWebhookHandler _webHookHandler;


        //private ClientMessenger _clientMessenger;


        public FacebookController(
            INlpChatbotsAppService nlpChatbotsAppService,
            ICacheManager cacheManager,
            NlpChatbotFunction nlpChatbotFunction,
            IChatbotMessageManager chatbotMessageManager,
            INlpFacebookUsersAppService nlpFacebookUsersAppService,
            AntiDDoS antiDDoS)
        {
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _cacheManager = cacheManager;
            _nlpChatbotFunction = nlpChatbotFunction;
            _chatbotMessageManager = chatbotMessageManager;
            _antiDDoS = antiDDoS;
            _nlpFacebookUsersAppService = nlpFacebookUsersAppService;
        }

        [AbpAllowAnonymous]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 20, TimeWindowSeconds: 60)]
        //[HttpPost, HttpGet]
        //[Route("Receive/{id?}")]
        //[HttpPost("{id?}"), HttpGet("{id?}")]
        [HttpPost("{id?}")]
        public async Task<IActionResult> Webhook([FromRoute] Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return NotFound();

                if (_antiDDoS.isLimited(id.ToString(), "FacebookGet", 10, 100))
                    return Problem(statusCode: StatusCodes.Status429TooManyRequests);

                var chatbotDto = _nlpChatbotFunction.GetChatbotDto(id);
                if (chatbotDto == null || chatbotDto.Disabled == true || chatbotDto.IsDeleted == true || chatbotDto.EnableFacebook == false)
                    return NotFound();

                _webHookHandler ??= new MessengerWebhookHandler(chatbotDto.FacebookVerifyToken, chatbotDto.FacebookSecretKey);
                //_clientMessenger ??= new ClientMessenger(chatbot.FacebookAccessToken, chatbot.FacebookSecretKey);

                IActionResult result = await FacebookAsync(chatbotDto);
                return result ?? BadRequest();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), ex);
                return BadRequest();
            }
        }


        /// <summary>
        /// Facebook the asynchronous.
        /// </summary>
        /// <returns></returns>
        private async Task<IActionResult> FacebookAsync(NlpChatbotDto chatbotDto)
        {
            return await _webHookHandler.HandleAsync(HttpContext, async (callback, s) =>
           {
               foreach (var entry in callback.Entry)
               {
                   foreach (var messaging in entry.Messaging)
                   {
                       if ((messaging.Message != null && !messaging.Message.IsEcho) || messaging.Postback != null)
                       {
                           /// User Send Message
                           /// This callback will occur when a message has been sent to your page.You may receive text messages or messages 
                           /// with attachments(image, audio, video, file or location).Callbacks contain a seq number which can be used 
                           /// to know the sequence of a message in a conversation. Messages are always sent in order.
                           /// You can subscribe to this callback by selecting the message field when setting up your webhook.
                           //await FacebookMessageAsync(messaging);

                           //var userProfile = await _clientMessenger.GetUserProfileAsync(messaging.Sender.Id);
                           //RILogManager.Default.SendJSON("userProfile", userProfile);

                           //var result = await _clientMessenger.SendMessageAsync(messaging.Sender.Id, new TextMessage
                           //{
                           //    Text = $"Hi, {userProfile.Name}. An agent will respond to your question shortly."
                           //});

                           //RILogManager.Default.SendJSON("Results", new[] { result });

                           var facebookUser = await _nlpFacebookUsersAppService.GetNlpFacebookUserDtoAsync(chatbotDto.Id, messaging.Sender.Id);

                           var input = new ChatbotMessageManagerMessageDto()
                           {
                               ReceiverRole = "chatbot",
                               ClientId = facebookUser.Id,
                               ChatbotId = chatbotDto.Id,
                               MessageType = "text",
                               ConnectionProtocol = "facebook",
                               ClientChannel = "facebook",
                               Message = messaging.Message?.Text ?? messaging.Postback?.Payload,
                               SenderImage = facebookUser.PictureUrl,
                               SenderName = facebookUser.UserName
                           };
                           await _chatbotMessageManager.ReceiveClientFacebookMessage(input);
                       }
                       else if (messaging.Postback != null)
                       {
                       }
                       else if (messaging.Delivery != null)
                       {
                           /// This callback will occur when a message a page has sent has been delivered.
                           /// You can subscribe to this callback by selecting the message_deliveries field when setting up your webhook.
                       }
                       else if (messaging.Read != null)
                       {
                           /// This callback will occur when a message a page has sent has been read by the user.
                           /// You can subscribe to this callback by selecting the message_reads field when setting up your webhook.
                       }
                       else if (messaging.Optin != null)
                       {
                           /// User Call "Message Us" 
                       }
                       else if (messaging.Referral != null)
                       {
                           /// Referral
                       }
                       else if (messaging.AccountLinking != null)
                       {
                           /// Account Linking
                       }
                   }
               }

               return Ok();
           });
        }
    }
}