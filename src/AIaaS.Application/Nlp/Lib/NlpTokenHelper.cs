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
using Abp.Dependency;
using System.Collections.Concurrent;
using Abp.Timing;
using Abp.Runtime.Caching;
using AIaaS.Helpers;

namespace AIaaS.Nlp
{
    public class NlpTokenHelper : ISingletonDependency
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRepository<NlpToken, Guid> _nlpTokenRepository;

        public NlpTokenHelper(
            ICacheManager cacheManager,
            IRepository<NlpToken, Guid> nlpTokenRepository)
        {
            _cacheManager = cacheManager;
            _nlpTokenRepository = nlpTokenRepository;
        }


        public bool IsValid(string tokenType, string tokenValue)
        {
            if (tokenType.IsNullOrEmpty() || tokenValue.IsNullOrEmpty())
                return false;

            var tokenData = GetTokenValue(tokenType);
            if (tokenData.IsNullOrEmpty() == true)
                return false;

            return tokenData == tokenValue;
        }

        public string GetTokenValue(string tokenType)
        {
            var tokenValue = (string)_cacheManager.Get_NlpToken(tokenType);
            if (tokenValue.IsNullOrEmpty())
            {
                tokenValue = _nlpTokenRepository.FirstOrDefault(e => e.NlpTokenType == tokenType)?.NlpTokenValue;

                if (tokenValue.IsNullOrEmpty() == false)
                    _cacheManager.Set_NlpToken(tokenType, tokenValue);
            }

            return tokenValue;
        }
    }
}