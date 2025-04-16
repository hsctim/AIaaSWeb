using AIaaS.Nlp;
using AIaaS.Authorization.Users;

using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
//using AIaaS.Nlp.Exporting;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using Abp.Application.Services.Dto;
using AIaaS.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.Application.Services;
using Abp.UI;
using Abp.Domain.Uow;
using ApiProtectorDotNet;
using Abp.Timing;

namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbMessages)]
    public class NlpCbMessagesAppService : AIaaSAppServiceBase, INlpCbMessagesAppService
    {
        private readonly IRepository<NlpCbMessage, Guid> _nlpCbMessageRepository;
        private readonly IRepository<NlpChatbot, Guid> _nlpChatbotRepository;
        private readonly IRepository<NlpClientInfo, Guid> _nlpClientInfoRepository;
        private readonly NlpCbSession _nlpCbSession;

        public NlpCbMessagesAppService(
            IRepository<NlpCbMessage, Guid> nlpCbMessageRepository,
            IRepository<NlpChatbot, Guid> nlpChatbotRepository,
            IRepository<NlpClientInfo, Guid> nlpClientInfoRepository,
            NlpCbSession nlpCbSession)
        {
            _nlpCbMessageRepository = nlpCbMessageRepository;
            _nlpChatbotRepository = nlpChatbotRepository;
            _nlpClientInfoRepository = nlpClientInfoRepository;
            _nlpCbSession = nlpCbSession;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PagedResultDto<GetNlpCbMessageForViewDto>> GetAll(GetAllNlpCbMessagesInput input)
        {
            if (input.MaxResultCount > AppConsts.MaxPageSize)
                throw new UserFriendlyException(L("Exception"));

            _nlpCbSession["ChatbotId"] = input.NlpChatbotId?.ToString();

            if (input.NlpChatbotId.HasValue == false)
                return new PagedResultDto<GetNlpCbMessageForViewDto>(0, new List<GetNlpCbMessageForViewDto>());

            input.Filter = input.Filter?.Trim();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.SoftDelete))
            {
                var filteredNlpCbMessages = _nlpCbMessageRepository.GetAll()
                        .Include(e => e.NlpChatbotFk)
                        .Include(e => e.NlpAgentFk)
                        .Include(e => e.NlpLineUserFk)
                        .Include(e => e.NlpFacebookUserFk)
                        .Include(e => e.QAFk).ThenInclude(e => e.CurrentWfStateFk).ThenInclude(e => e.NlpWorkflowFk)
                        .Include(e => e.QAFk).ThenInclude(e => e.NextWfStateFk).ThenInclude(e => e.NlpWorkflowFk)
                        //.WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.NlpMessage.Contains(input.Filter) || (e.ClientId != null && e.ClientId.ToString().Contains(input.Filter)) || (e.ClientId != null && e.NlpLineUserFk != null && e.NlpLineUserFk.UserName.Contains(input.Filter)) || (e.ClientId != null && e.NlpFacebookUserFk != null && e.NlpFacebookUserFk.UserName.Contains(input.Filter)))
                        .WhereIf(input.MinNlpSentTimeFilter != null, e => e.NlpCreationTime >= input.MinNlpSentTimeFilter)
                        .WhereIf(input.MaxNlpSentTimeFilter != null, e => e.NlpCreationTime <= input.MaxNlpSentTimeFilter)
                        .Where(e => e.NlpChatbotId == input.NlpChatbotId)
                        .Where(e => e.NlpCreationTime >= Clock.Now.AddMonths(-12));

                var filter2 = from o in filteredNlpCbMessages
                              join o1 in _nlpClientInfoRepository.GetAll() on o.ClientId equals o1.ClientId into j1
                              from s1 in j1.DefaultIfEmpty()
                              where (string.IsNullOrWhiteSpace(input.Filter) == true || o.NlpMessage.Contains(input.Filter) || o.NlpAgentFk.Name.Contains(input.Filter) || o.ClientId.ToString().Contains(input.Filter) || o.NlpLineUserFk.UserName.Contains(input.Filter) || o.NlpFacebookUserFk.UserName.Contains(input.Filter) || (s1 != null && s1.ClientChannel.Contains(input.Filter)))
                              select new GetNlpCbMessageForViewDto()
                              {
                                  NlpMessage = o.NlpMessage,
                                  NlpCreationTime = o.NlpCreationTime,
                                  NlpChatbotName = o.NlpChatbotFk.Name,
                                  NlpCbAgentName = o.NlpAgentFk.Name,
                                  NlpClientName = o.NlpLineUserFk.UserName + o.NlpFacebookUserFk.UserName,
                                  ClientId = o.ClientId,
                                  NlpCbSentType = (o.NlpSenderRole == "chatbot" || o.NlpSenderRole == "agent") ? "send" : "Receive",

                                  NlpCbSenderRoleName = o.NlpSenderRole,
                                  NlpCbReceiverRoleName = o.NlpReceiverRole,
                                  ChannelName = s1.ClientChannel,

                                  PriorWfS = string.IsNullOrEmpty(o.QAFk.CurrentWfStateFk.StateName) ? null : o.QAFk.CurrentWfStateFk.NlpWorkflowFk.Name + " : " + o.QAFk.CurrentWfStateFk.StateName,
                                  CurrentWfS = string.IsNullOrEmpty(o.QAFk.NextWfStateFk.StateName) ? null : o.QAFk.NextWfStateFk.NlpWorkflowFk.Name + " : " + o.QAFk.NextWfStateFk.StateName,
                              };

                if (String.IsNullOrEmpty(input.Sorting) == false && input.Sorting.Contains("NlpCbSentType"))
                {
                    if (input.Sorting.Contains("asc"))
                        input.Sorting = "NlpCbSenderRoleName asc,NlpCbReceiverRoleName asc";
                    else
                        input.Sorting = "NlpCbSenderRoleName desc,NlpCbReceiverRoleName desc";
                }

                var totalCount = await filter2.CountAsync();

                var pagedAndFilteredNlpCbMessages = filter2
                    .OrderBy(input.Sorting ?? "NlpCreationTime desc")
                    .PageBy(input);

                return new PagedResultDto<GetNlpCbMessageForViewDto>(totalCount, await pagedAndFilteredNlpCbMessages.ToListAsync());
            }
        }

        [RemoteService(false)]
        public async Task<List<NlpChatbotDto>> GetAllForSelectList()
        {
            DateTime dt12m = Clock.Now.AddMonths(-12);

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.SoftDelete))
            {
                var _messages = _nlpCbMessageRepository.GetAll()
                    .Where(e => e.TenantId == AbpSession.TenantId && e.NlpCreationTime > dt12m && e.NlpChatbotId.HasValue)
                    .Select(e=>e.NlpChatbotId.Value)
                    .Distinct();
                var _chatbots = _nlpChatbotRepository.GetAll()
                    .Where(e => e.TenantId == AbpSession.TenantId);

                var r = from m in _messages
                        join o in _chatbots on m equals o.Id 
                        select new NlpChatbotDto
                        {
                            Id = o.Id,
                            Name = o.Name
                        };

                return await r.Distinct().OrderBy(e=>e.Name).ToListAsync();
            }
        }
    }
}