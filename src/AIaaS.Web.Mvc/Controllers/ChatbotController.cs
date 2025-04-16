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
using System.Threading;
using Abp.Timing;
using Microsoft.AspNetCore.Http;
using AIaaS.Nlp.Dtos;

namespace AIaaS.Web.Controllers
{
    [AbpAllowAnonymous]
    [DisableAuditing]
    public class ChatbotController : AIaaSControllerBase
    {
        private static readonly Guid _defaultBotPictureId = new Guid("00000000-0000-0000-0000-000000000000");

        private const int _SemaphoreSlimWaitTimeOut = 60000;

        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly ICacheManager _cacheManager;
        private readonly IProfileAppService _profileAppService;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly IChatbotMessageManager _chatbotMessageManager;
        private readonly AntiDDoS _antiDDoS;
        private readonly INlpPolicyAppService _nlpPolicyAppService;

        public ChatbotController(
            INlpChatbotsAppService nlpChatbotsAppService,
            ICacheManager cacheManager,
            IProfileAppService profileAppService,
            NlpChatbotFunction nlpChatbotFunction,
            IChatbotMessageManager chatbotMessageManager,
            AntiDDoS antiDDoS,
            INlpPolicyAppService nlpPolicyAppService)
        {
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _cacheManager = cacheManager;
            _profileAppService = profileAppService;
            _nlpChatbotFunction = nlpChatbotFunction;
            _chatbotMessageManager = chatbotMessageManager;
            _antiDDoS = antiDDoS;
            _nlpPolicyAppService = nlpPolicyAppService;
        }

        [AbpAllowAnonymous]
        [RemoteService(false)]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        [DisableAuditing]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 60, TimeWindowSeconds: 60, PenaltySeconds: 60)]
        public async Task<ActionResult> ProfilePicture(Guid? id)
        {
            id ??= _defaultBotPictureId;

            var fileResult = (FileResult)_cacheManager.Get_BotPicture(id.Value);

            if (fileResult != null)
                return fileResult;

            var data = await _nlpChatbotsAppService.GetProfilePicture(id.Value);
            //if (data == null && id.HasValue)
            //{
            //    var chatbot = _nlpChatbotFunction.GetChatbotDto(id.Value);

            //    if (chatbot != null && chatbot.ChatbotPictureId != null)
            //        data = await _nlpChatbotsAppService.GetProfilePicture(chatbot.ChatbotPictureId.Value);
            //}

            data ??= await _nlpChatbotsAppService.GetProfilePicture(_defaultBotPictureId);

            fileResult = File(data, MimeTypeNames.ImageJpeg);
            fileResult.LastModified = Clock.Now;
            _cacheManager.Set_BotPicture(id.Value, fileResult);

            return fileResult;
        }


        [AbpAllowAnonymous]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        [DisableAuditing]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 60, TimeWindowSeconds: 60, PenaltySeconds: 60)]
        public JsonResult SessionInfo(Guid? id)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();
            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(id.Value);

            if (nlpChatbot == null || nlpChatbot.IsDeleted == true || nlpChatbot.Disabled == true || nlpChatbot.EnableWebChat == false)
                return Json(null);

            info["Name"] = nlpChatbot.Name;
            info["Language"] = nlpChatbot.Language;
            info["GreetingMsg"] = nlpChatbot.GreetingMsg?.Replace("@{Chatbot.Name}@", nlpChatbot.Name);
            info["AlternativeQuestion"] = nlpChatbot.AlternativeQuestion;

            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(nlpChatbot.Language);
            info["TypeMessage"] = L("NlpChatroomTypeMessage", cultureInfo);

            return Json(info);
        }

        [AbpAllowAnonymous]
        [DisableAuditing]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 60, TimeWindowSeconds: 60, PenaltySeconds: 60)]
        public async Task<ActionResult> SendMessage([FromBody] ChatbotMessageManagerMessageDto input)
        {
            try
            {
                input.ReceiverRole = "chatbot";

                if (input.ClientId == null)
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidClientId, "ClientId should be a valid guid.");

                if (input.ChatbotId == null)
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "ChatbotId should be a valid guid.");

                _cacheManager.Set_ChatbotControllerMutex(input.ChatbotId.Value, input.ClientId.Value, 10);

                var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();

                if (_antiDDoS.isLimited(input.ChatbotId.ToString() + input.ClientId.ToString() + remoteIpAddress, "GetMessage", 60, 60))
                    return Json(new
                    {
                        errorCode = StatusCodes.Status429TooManyRequests,
                        errorMessage = "HTTP/429"
                    });

                if (_nlpChatbotFunction.IsWebAPIEnabled(input.ChatbotId.Value) == false)
                    return Json(new 
                    {
                        errorCode = StatusCodes.Status400BadRequest,
                        errorMessage = "The Chatbot WebAPI is disabled",
                    });

                input.ClientIP = remoteIpAddress;
                if (input.Message.IsNullOrEmpty() || input.Message.Trim().IsNullOrEmpty())
                    throw new UserFriendlyException(ChatErrorCode.Error_NoMessage, "No Message");

                input.MessageType = "text";
                input.ConnectionProtocol = "http";

                var chatbotMessageManagerMessageDto = await _chatbotMessageManager.ReceiveClientHttpMessage(input);

                return GetJsonResultFromMessages(chatbotMessageManagerMessageDto);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message, ex);
                return Json(new
                {
                    errorCode = StatusCodes.Status500InternalServerError,
                    errorMessage = ex.Message,
                });
            }
            finally
            {
                try
                {
                    _cacheManager.Set_ChatbotController_GetMessages_HasData(_nlpChatbotFunction.GetTenantId(input.ChatbotId.Value), input.ClientId.Value, false);
                    _cacheManager.Set_ChatbotControllerMutex(input.ChatbotId.Value, input.ClientId.Value, 1);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.Message, ex);
                }
            }
        }


        [AbpAllowAnonymous]
        [DisableAuditing]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 60, TimeWindowSeconds: 60, PenaltySeconds: 60)]
        public async Task<ActionResult> GetMessage([FromBody] ChatbotMessageManagerMessageDto input)
        {
            SemaphoreSlim semaphoreSlim= null;

            try
            {
                if (input.ClientId == null)
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidClientId, "ClientId should be a valid guid.");

                if (input.ChatbotId == null)
                    throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "ChatbotId should be a valid guid.");

                if (_antiDDoS.isLimited(input.ChatbotId.ToString() + input.ClientId.ToString() + Request.HttpContext.Connection.RemoteIpAddress.ToString(), "GetMessage", 60, 60))
                {
                    await Task.Delay(5000);
                    return Json(new
                    {
                        errorCode = StatusCodes.Status429TooManyRequests,
                        errorMessage = "HTTP/429"
                    });
                }

                if (_nlpChatbotFunction.IsWebAPIEnabled(input.ChatbotId.Value) == false)
                {
                    await Task.Delay(5000);
                    return Json(new
                    {
                        errorCode = StatusCodes.Status400BadRequest,
                        errorMessage = "The Chatbot WebAPI is disabled",
                    });
                }

                if (input.WaitingTimeOut == 0)
                    input.WaitingTimeOut = 60000;

                var waitingTimeOut = Math.Min(Math.Max(1000, input.WaitingTimeOut),60000);
                int startTickCount = Environment.TickCount;
                //long polling 

                semaphoreSlim = await _nlpPolicyAppService.GetMessageSendQuotaSemaphoreSlim(_nlpChatbotFunction.GetTenantId(input.ChatbotId.Value));

                if ((await semaphoreSlim.WaitAsync(_SemaphoreSlimWaitTimeOut)) == false)
                    return Json(new
                    {
                        errorCode = StatusCodes.Status503ServiceUnavailable,
                        errorMessage = "503 Service Unavailable",
                    });


                for (int n = 0; n < 60; n++)
                {
                    int nValue = _cacheManager.Get_ChatbotControllerMutex(input.ChatbotId.Value, input.ClientId.Value);

                    if (nValue > 0)
                    {
                        _cacheManager.Set_ChatbotControllerMutex(input.ChatbotId.Value, input.ClientId.Value, nValue - 1);
                        await Task.Delay(1000);
                        continue;
                    }

                    if (_cacheManager.Get_ChatbotController_GetMessages_HasData_AutoReset(_nlpChatbotFunction.GetTenantId(input.ChatbotId.Value), input.ClientId.Value) == true)
                    {
                        input.MessageType = "text";
                        input.ReceiverRole = "chatbot";
                        input.ConnectionProtocol = "http";

                        var chatbotMessageManagerMessageDto = await _chatbotMessageManager.GetMessagesByHttp(input);

                        if (chatbotMessageManagerMessageDto.Count > 0)
                            return GetJsonResultFromMessages(chatbotMessageManagerMessageDto);
                    }

                    if (HttpContext.RequestAborted.IsCancellationRequested)
                        break;

                    if (Environment.TickCount-startTickCount > waitingTimeOut)
                        break;

                    await Task.Delay(1000);
                }

                return Json( new {});
            }
            catch (Exception ex)
            {
                await Task.Delay(5000);
                Logger.Fatal(ex.Message, ex);
                return Json( new
                {
                    errorCode = StatusCodes.Status500InternalServerError,
                    errorMessage = ex.Message,
                });
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


        [AbpAllowAnonymous]
        [DisableAuditing]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 60, TimeWindowSeconds: 60, PenaltySeconds: 60)]
        public async Task<ActionResult> SetWorkflowState([FromBody] SetChatroomWorkflow input)
        {
            //return await SetWorkflowState(input);
            try
            {
                var output = await _chatbotMessageManager.SetChatbotWorkflowState(input);
                return Json(output);
            }
            catch (Exception ex)
            {
                await Task.Delay(5000);
                Logger.Fatal(ex.Message, ex);
                return Json(new
                {
                    errorCode = StatusCodes.Status500InternalServerError,
                    errorMessage = ex.Message,
                });
            }
        }



        [AbpAllowAnonymous]
        [DisableAuditing]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 60, TimeWindowSeconds: 60, PenaltySeconds: 60)]
        public ActionResult GetChatbotInfo(Guid id)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(id);

            if (chatbot.EnableWebChat == false)
                return Json(new
                {
                    errorCode = StatusCodes.Status400BadRequest,
                    errorMessage = "The Chatbot WebAPI is disabled",
                });

            var d = new Dictionary<string, object>();

            if (chatbot != null && chatbot.IsDeleted == false)
            {
                d["Id"] = chatbot.Id;
                d["Name"] = chatbot.Name;
                d["Language"] = chatbot.Language;
                d["PictureId"] = chatbot.ChatbotPictureId;
                d["PictureURL"] = "/Chatbot/ProfilePicture/" + chatbot.ChatbotPictureId.ToString();
                d["Disabled"] = chatbot.Disabled;

                if (chatbot.FailedMsg.IsNullOrEmpty() == false)
                    d["FailedMsg"] = _chatbotMessageManager.ReplaceCustomStringAsync(chatbot.FailedMsg, id);

                if (chatbot.GreetingMsg.IsNullOrEmpty() == false)
                    d["GreetingMsg"] = _chatbotMessageManager.ReplaceCustomStringAsync(chatbot.GreetingMsg, id);

                if (chatbot.AlternativeQuestion.IsNullOrEmpty() == false)
                    d["AlternativeQuestion"] = _chatbotMessageManager.ReplaceCustomStringAsync(chatbot.AlternativeQuestion, id);
            }

            return Json(d);
        }

        [AbpAllowAnonymous]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        [DisableAuditing]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 60, TimeWindowSeconds: 60, PenaltySeconds: 60)]
        public async Task<FileResult> GetProfilePictureById(Guid? id)
        {
            if (id == null)
                return GetDefaultProfilePictureInternal();

            var fileResult = (FileResult)_cacheManager.Get_UserProfilePicture_By_PicId(id.Value);
            if (fileResult != null)
                return fileResult;

            Byte[] data = null;

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MustHaveTenant, AbpDataFilters.MayHaveTenant))
            {
                data = await _profileAppService.GetProfilePictureByIdOrNull(id.Value);
                if (data == null)
                    return GetDefaultProfilePictureInternal();
            }

            fileResult = File(data, MimeTypeNames.ImageJpeg);
            fileResult.LastModified = Clock.Now;

            _cacheManager.Set_UserProfilePicture_By_PicId(id.Value, fileResult);

            return fileResult;
        }

        private FileResult GetDefaultProfilePictureInternal()
        {
            return File(Path.Combine("Common", "Images", "default-profile-picture.png"), MimeTypeNames.ImagePng);
        }

        private JsonResult GetJsonResultFromMessages(IList<ChatbotMessageManagerMessageDto> msgs)
        {
            return Json( new
            {
                messages =msgs
            });
        }
    }
}