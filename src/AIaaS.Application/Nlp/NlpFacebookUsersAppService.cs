using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using Abp.Application.Services.Dto;
using AIaaS.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using AIaaS.Storage;
using Abp.Runtime.Caching;
using Abp.Auditing;
using Abp.Application.Services;
using AIaaS.Helpers;
using ReflectSoftware.Facebook.Messenger.AspNetCore.Webhook;
using ReflectSoftware.Facebook.Messenger.Client;

namespace AIaaS.Nlp
{
    [RemoteService(false)]
    [DisableAuditing]
    public class NlpFacebookUsersAppService : AIaaSAppServiceBase, INlpFacebookUsersAppService
    {
        private readonly IRepository<NlpFacebookUser, Guid> _nlpFacebookUserRepository;
        private readonly ICacheManager _cacheManager;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private MessengerWebhookHandler _webHookHandler;
        private ClientMessenger _clientMessenger;
        private NlpFacebookUserDto __nlpFacebookUserDtoCache;

        public NlpFacebookUsersAppService(IRepository<NlpFacebookUser, Guid> nlpFacebookUserRepository,
            NlpChatbotFunction nlpChatbotFunction,
            ICacheManager cacheManager)
        {
            _nlpFacebookUserRepository = nlpFacebookUserRepository;
            _nlpChatbotFunction = nlpChatbotFunction;
            _cacheManager = cacheManager;
            __nlpFacebookUserDtoCache = null;
        }

        [DisableAuditing]
        public NlpFacebookUserDto GetNlpFacebookUserDto(Guid clientId)
        {
            if (__nlpFacebookUserDtoCache != null && __nlpFacebookUserDtoCache.Id == clientId)
                return __nlpFacebookUserDtoCache;

            __nlpFacebookUserDtoCache = (NlpFacebookUserDto)_cacheManager.Get_NlpFacebookUserDto(clientId);

            if (__nlpFacebookUserDtoCache != null)
            {
                if (__nlpFacebookUserDtoCache.PictureUrl.IsNullOrEmpty())
                {
                    var FacebookUser = (NlpFacebookUserDto)_cacheManager.Get_NlpFacebookUserDto(__nlpFacebookUserDtoCache.UserId);
                    if (FacebookUser != null)
                    {
                        __nlpFacebookUserDtoCache = FacebookUser;
                        _cacheManager.Set_NlpFacebookUserDto(clientId, __nlpFacebookUserDtoCache);
                    }
                }

                return __nlpFacebookUserDtoCache;
            }

            __nlpFacebookUserDtoCache = ObjectMapper.Map<NlpFacebookUserDto>(_nlpFacebookUserRepository.FirstOrDefault(c => c.Id == clientId));

            if (__nlpFacebookUserDtoCache != null)
                _cacheManager.Set_NlpFacebookUserDto(clientId, __nlpFacebookUserDtoCache);

            return __nlpFacebookUserDtoCache;
        }

        [DisableAuditing]
        public async Task<NlpFacebookUserDto> GetNlpFacebookUserDtoAsync(Guid chatbotId, string facebookUserId)
        {
            try
            {
                if (__nlpFacebookUserDtoCache != null && __nlpFacebookUserDtoCache.UserId == facebookUserId)
                    return __nlpFacebookUserDtoCache;

                __nlpFacebookUserDtoCache = (NlpFacebookUserDto)_cacheManager.Get_NlpFacebookUserDto(facebookUserId);

                if (__nlpFacebookUserDtoCache != null)
                    return __nlpFacebookUserDtoCache;

                __nlpFacebookUserDtoCache ??= ObjectMapper.Map<NlpFacebookUserDto>(_nlpFacebookUserRepository.FirstOrDefault(c => c.UserId == facebookUserId));
                //updated userName and picture
                var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);

                _webHookHandler ??= new MessengerWebhookHandler(chatbot.FacebookVerifyToken, chatbot.FacebookSecretKey);
                _clientMessenger ??= new ClientMessenger(chatbot.FacebookAccessToken,
                    chatbot.FacebookSecretKey);

                var user = await _clientMessenger.GetUserProfileAsync(facebookUserId);

                //var user = isRock.FacebookBot.Utility.GetUserInfo(FacebookUserId, channelAccessToken);
                bool bUpdateCache = false;

                if (__nlpFacebookUserDtoCache == null && user != null)
                {
                    bUpdateCache = true;
                    __nlpFacebookUserDtoCache = ObjectMapper.Map<NlpFacebookUserDto>(_nlpFacebookUserRepository.Insert(new NlpFacebookUser()
                    {
                        UserId = facebookUserId,
                        UserName = user.Name,
                        PictureUrl = user.ProfilePicture
                    }));
                }
                else if (__nlpFacebookUserDtoCache != null && user != null)
                {
                    if (__nlpFacebookUserDtoCache.UserName != user.Name || __nlpFacebookUserDtoCache.PictureUrl != user.ProfilePicture)
                    {
                        __nlpFacebookUserDtoCache.UserName = user.Name;
                        __nlpFacebookUserDtoCache.PictureUrl = user.ProfilePicture;
                        bUpdateCache = true;

                        _nlpFacebookUserRepository.Update(ObjectMapper.Map<NlpFacebookUser>(__nlpFacebookUserDtoCache));
                    }
                }

                if (__nlpFacebookUserDtoCache != null && bUpdateCache == true)
                {
                    _cacheManager.Set_NlpFacebookUserDto(facebookUserId, __nlpFacebookUserDtoCache);
                    _cacheManager.Set_NlpFacebookUserDto(__nlpFacebookUserDtoCache.Id, __nlpFacebookUserDtoCache);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return __nlpFacebookUserDtoCache;
        }
    }
}