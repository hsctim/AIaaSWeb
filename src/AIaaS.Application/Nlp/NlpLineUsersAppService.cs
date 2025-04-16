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
using Abp.Application.Services;
using Abp.Runtime.Caching;
using AIaaS.Helpers;
using Abp.Domain.Uow;
using Abp.Auditing;

namespace AIaaS.Nlp
{
    [RemoteService(false)]
    [DisableAuditing]
    public class NlpLineUsersAppService : AIaaSAppServiceBase, INlpLineUsersAppService
    {
        private readonly IRepository<NlpLineUser, Guid> _nlpLineUserRepository;
        private readonly ICacheManager _cacheManager;
        private NlpLineUserDto __nlpLineUserDtoCache;

        public NlpLineUsersAppService(IRepository<NlpLineUser, Guid> nlpLineUserRepository,
            ICacheManager cacheManager)
        {
            _nlpLineUserRepository = nlpLineUserRepository;
            _cacheManager = cacheManager;
            __nlpLineUserDtoCache = null;
        }

        [DisableAuditing]
        public NlpLineUserDto GetNlpLineUserDto(Guid clientId)
        {
            if (__nlpLineUserDtoCache != null && __nlpLineUserDtoCache.Id == clientId)
                return __nlpLineUserDtoCache;

            __nlpLineUserDtoCache = (NlpLineUserDto)_cacheManager.Get_NlpLineUserDto(clientId);

            if (__nlpLineUserDtoCache != null)
            {
                if (__nlpLineUserDtoCache.PictureUrl.IsNullOrEmpty())
                {
                    var lineUser = (NlpLineUserDto)_cacheManager.Get_NlpLineUserDto(__nlpLineUserDtoCache.UserId);
                    if (lineUser != null)
                    {
                        __nlpLineUserDtoCache = lineUser;
                        _cacheManager.Set_NlpLineUserDto(clientId, __nlpLineUserDtoCache);
                    }
                }

                return __nlpLineUserDtoCache;
            }

            __nlpLineUserDtoCache = ObjectMapper.Map<NlpLineUserDto>(_nlpLineUserRepository.FirstOrDefault(c => c.Id == clientId));

            if (__nlpLineUserDtoCache != null)
                _cacheManager.Set_NlpLineUserDto(clientId, __nlpLineUserDtoCache);

            return __nlpLineUserDtoCache;
        }

        [DisableAuditing]
        public NlpLineUserDto GetNlpLineUserDto(string lineUserId, string channelAccessToken)
        {
            if (__nlpLineUserDtoCache != null && __nlpLineUserDtoCache.UserId == lineUserId)
                return __nlpLineUserDtoCache;

            __nlpLineUserDtoCache = (NlpLineUserDto)_cacheManager.Get_NlpLineUserDto(lineUserId);

            if (__nlpLineUserDtoCache != null)
                return __nlpLineUserDtoCache;

            __nlpLineUserDtoCache ??= ObjectMapper.Map<NlpLineUserDto>(_nlpLineUserRepository.FirstOrDefault(c => c.UserId == lineUserId));
            //updated userName and 
            var user = isRock.LineBot.Utility.GetUserInfo(lineUserId, channelAccessToken);
            bool bUpdateCache = false;

            if (__nlpLineUserDtoCache == null && user != null)
            {
                bUpdateCache = true;
                __nlpLineUserDtoCache = ObjectMapper.Map<NlpLineUserDto>(_nlpLineUserRepository.Insert(new NlpLineUser()
                {
                    UserId = user.userId,
                    UserName = user.displayName,
                    PictureUrl = user.pictureUrl
                }));
            }
            else if (__nlpLineUserDtoCache != null && user != null)
            {
                if (__nlpLineUserDtoCache.UserName != user.displayName || __nlpLineUserDtoCache.PictureUrl != user.pictureUrl)
                {
                    __nlpLineUserDtoCache.UserName = user.displayName;
                    __nlpLineUserDtoCache.PictureUrl = user.pictureUrl;
                    bUpdateCache = true;

                    _nlpLineUserRepository.Update(ObjectMapper.Map<NlpLineUser>(__nlpLineUserDtoCache));
                }
            }

            if (__nlpLineUserDtoCache != null && bUpdateCache == true)
            {
                _cacheManager.Set_NlpLineUserDto(lineUserId, __nlpLineUserDtoCache);
                _cacheManager.Set_NlpLineUserDto(__nlpLineUserDtoCache.Id, __nlpLineUserDtoCache);
            }

            return __nlpLineUserDtoCache;
        }
    }
}