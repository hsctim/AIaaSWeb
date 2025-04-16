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
using ApiProtectorDotNet;
using Abp.Runtime.Caching;
using AIaaS.Helpers;

namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpTokens)]
    public class NlpTokensAppService : AIaaSAppServiceBase, INlpTokensAppService
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRepository<NlpToken, Guid> _nlpTokenRepository;

        public NlpTokensAppService(
            ICacheManager cacheManager,
            IRepository<NlpToken, Guid> nlpTokenRepository)
        {
            _cacheManager = cacheManager;
            _nlpTokenRepository = nlpTokenRepository;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PagedResultDto<GetNlpTokenForViewDto>> GetAll(GetAllNlpTokensInput input)
        {
            if (input.MaxResultCount > AppConsts.MaxPageSize)
                throw new UserFriendlyException(L("Exception"));

            var filteredNlpTokens = _nlpTokenRepository.GetAll()
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.NlpTokenType.Contains(input.Filter.Trim()) || e.NlpTokenValue.Contains(input.Filter.Trim()));

            var pagedAndFilteredNlpTokens = filteredNlpTokens
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var nlpTokens = from o in pagedAndFilteredNlpTokens
                            select new GetNlpTokenForViewDto()
                            {
                                NlpToken = new NlpTokenDto
                                {
                                    NlpTokenType = o.NlpTokenType,
                                    NlpTokenValue = o.NlpTokenValue,
                                    Id = o.Id
                                }
                            };

            var totalCount = await filteredNlpTokens.CountAsync();

            return new PagedResultDto<GetNlpTokenForViewDto>(
                totalCount, await nlpTokens.ToListAsync()
            );
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpTokens_Edit)]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetNlpTokenForEditOutput GetNlpTokenForEdit(EntityDto<Guid> input)
        {
            var nlpToken = _nlpTokenRepository.FirstOrDefault(input.Id);

            var output = new GetNlpTokenForEditOutput { NlpToken = ObjectMapper.Map<CreateOrEditNlpTokenDto>(nlpToken) };

            return output;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public void CreateOrEdit(CreateOrEditNlpTokenDto input)
        {
            if (input.Id == null)
            {
                Create(input);
            }
            else
            {
                Update(input);
            }
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpTokens_Create)]
        protected virtual void Create(CreateOrEditNlpTokenDto input)
        {
            var nlpToken = ObjectMapper.Map<NlpToken>(input);

            _nlpTokenRepository.Insert(nlpToken);
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpTokens_Edit)]
        protected virtual void Update(CreateOrEditNlpTokenDto input)
        {
            var nlpToken = _nlpTokenRepository.FirstOrDefault((Guid)input.Id);
            ObjectMapper.Map(input, nlpToken);

            _cacheManager.Remove_NlpToken(nlpToken.NlpTokenType);
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpTokens_Delete)]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public void Delete(EntityDto<Guid> input)
        {
            var nlpToken = _nlpTokenRepository.FirstOrDefault((Guid)input.Id);
            if (nlpToken != null && nlpToken.NlpTokenType.IsNullOrEmpty() == false)
                _cacheManager.Remove_NlpToken(nlpToken.NlpTokenType);

            _nlpTokenRepository.Delete(input.Id);
        }
    }
}