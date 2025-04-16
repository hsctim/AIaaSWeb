using AIaaS.Nlp;

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
using Newtonsoft.Json;
using System.Threading;
using Abp.Runtime.Caching;
using AIaaS.Helpers;
using AIaaS.Nlp.Lib;

namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows)]
    public class NlpWorkflowStatesAppService : AIaaSAppServiceBase, INlpWorkflowStatesAppService
    {
        private readonly IRepository<NlpWorkflowState, Guid> _nlpWorkflowStateRepository;
        private readonly IRepository<NlpWorkflow, Guid> _lookup_nlpWorkflowRepository;
        private readonly ICacheManager _cacheManager;

        public NlpWorkflowStatesAppService(IRepository<NlpWorkflowState, Guid> nlpWorkflowStateRepository, IRepository<NlpWorkflow, Guid> lookup_nlpWorkflowRepository,
            ICacheManager cacheManager)
        {
            _nlpWorkflowStateRepository = nlpWorkflowStateRepository;
            _lookup_nlpWorkflowRepository = lookup_nlpWorkflowRepository;
            _cacheManager = cacheManager;
        }

        public async Task<PagedResultDto<GetNlpWorkflowStateForViewDto>> GetAll(GetAllNlpWorkflowStatesInput input)
        {
            if (input.NlpWorkflowId == Guid.Empty)
                throw new UserFriendlyException(L("ErrorWorkFlowId"));

            var filteredNlpWorkflowStates = _nlpWorkflowStateRepository.GetAll()
                        .Include(e => e.NlpWorkflowFk).ThenInclude(e => e.NlpChatbotFk)
                        .Where(e => e.NlpWorkflowFk != null && e.NlpWorkflowFk.Disabled == false && e.NlpWorkflowFk.NlpChatbotFk != null && e.NlpWorkflowFk.NlpChatbotFk.Disabled == false && input.NlpWorkflowId == e.NlpWorkflowId);

            var pagedAndFilteredNlpWorkflowStates = filteredNlpWorkflowStates
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var nlpWorkflowStates = from o in pagedAndFilteredNlpWorkflowStates
                                    join o1 in _lookup_nlpWorkflowRepository.GetAll() on o.NlpWorkflowId equals o1.Id into j1
                                    from s1 in j1.DefaultIfEmpty()

                                    select new
                                    {
                                        o.StateName,
                                        o.StateInstruction,
                                        o.ResponseNonWorkflowAnswer,
                                        o.DontResponseNonWorkflowErrorAnswer,
                                        Id = o.Id,
                                        NlpWorkflowName = s1.Name.ToString(),
                                        NlpChatbotName = s1.NlpChatbotFk.Name.ToString(),
                                    };

            var totalCount = await filteredNlpWorkflowStates.CountAsync();

            var dbList = await nlpWorkflowStates.ToListAsync();
            var results = new List<GetNlpWorkflowStateForViewDto>();

            foreach (var o in dbList)
            {
                var res = new GetNlpWorkflowStateForViewDto()
                {
                    NlpWorkflowState = new NlpWorkflowStateDto
                    {
                        StateName = o.StateName,
                        StateInstruction = o.StateInstruction,
                        ResponseNonWorkflowAnswer = o.ResponseNonWorkflowAnswer,
                        DontResponseNonWorkflowErrorAnswer = o.DontResponseNonWorkflowErrorAnswer,
                        Id = o.Id,
                    },
                    NlpWorkflowName = o.NlpWorkflowName,
                    NlpChatbotName = o.NlpChatbotName
                };

                results.Add(res);
            }

            return new PagedResultDto<GetNlpWorkflowStateForViewDto>(
                totalCount,
                results
            );
        }


        public async Task<GetNlpWorkflowStateForEditOutput> GetNlpWorkflowStateForEdit(EntityDto<Guid> input)
        {
            var nlpWorkflowState = await _nlpWorkflowStateRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetNlpWorkflowStateForEditOutput
            {
                NlpWorkflowState = ObjectMapper.Map<CreateOrEditNlpWorkflowStateDto>(nlpWorkflowState)
            };

            var _lookupNlpWorkflow = await _lookup_nlpWorkflowRepository
                .GetAllIncluding(e => e.NlpChatbotFk)
                .Where(e => e.Id == output.NlpWorkflowState.NlpWorkflowId)
                .FirstOrDefaultAsync();

            output.NlpWorkflowName = _lookupNlpWorkflow.Name?.ToString();
            output.NlpChatbotName = _lookupNlpWorkflow.NlpChatbotFk.Name?.ToString();

            return output;
        }

        public async Task CreateOrEdit(CreateOrEditNlpWorkflowStateDto input)
        {
            input.OutgoingFalseOp = JsonConvert.SerializeObject(new NlpWfsFalsePredictionOpDto()
            {
                NextStatus = input.FalsePredict1Select,
                ResponseMsg = input.FalsePredict1ResponseMsg
            });

            input.Outgoing3FalseOp = JsonConvert.SerializeObject(new NlpWfsFalsePredictionOpDto()
            {
                NextStatus = input.FalsePredict3Select,
                ResponseMsg = input.FalsePredict3ResponseMsg
            });

            input.StateName = input.StateName?.Trim();

            if (input.Id == null)
            {
                await Create(input);
            }
            else
            {
                await Update(input);
            }
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Create)]
        protected virtual async Task Create(CreateOrEditNlpWorkflowStateDto input)
        {
            var nlpWorkflowState = ObjectMapper.Map<NlpWorkflowState>(input);
            nlpWorkflowState.Vector = JsonConvert.SerializeObject(NlpHelper.CreateNewVector());

            if (AbpSession.TenantId != null)
            {
                nlpWorkflowState.TenantId = (int)AbpSession.TenantId;
            }

            await _nlpWorkflowStateRepository.InsertAsync(nlpWorkflowState);

            var workflow = await _lookup_nlpWorkflowRepository.FirstOrDefaultAsync(nlpWorkflowState.NlpWorkflowId);
            _cacheManager.Remove_NlpCbDictWofkflowState(workflow.NlpChatbotId);

            _cacheManager.Remove_NlpWorkflowStates(nlpWorkflowState.Id);
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Edit)]
        protected virtual async Task Update(CreateOrEditNlpWorkflowStateDto input)
        {
            var nlpWorkflowState = await _nlpWorkflowStateRepository.FirstOrDefaultAsync((Guid)input.Id);

            int[] vector;
            try
            {
                vector = JsonConvert.DeserializeObject<int[]>(nlpWorkflowState.Vector);
                if (vector.Length != 300)
                    throw new Exception();
            }
            catch (Exception)
            {
                vector = NlpHelper.CreateNewVector();
            }

            ObjectMapper.Map(input, nlpWorkflowState);
            nlpWorkflowState.Vector = JsonConvert.SerializeObject(vector);

            var workflow = await _lookup_nlpWorkflowRepository.FirstOrDefaultAsync(nlpWorkflowState.NlpWorkflowId);
            _cacheManager.Remove_NlpCbDictWofkflowState(workflow.NlpChatbotId);

            _cacheManager.Remove_NlpWorkflowStates(nlpWorkflowState.Id);
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Delete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            await _nlpWorkflowStateRepository.DeleteAsync(input.Id);
        }

        [RemoteService(false)]
        public async Task<List<NlpLookupTableDto>> GetAllNlpWorkflowStateForTableDropdown()
        {
            var list = await _nlpWorkflowStateRepository.GetAll()
                .Include(e => e.NlpWorkflowFk)
                .Select(workflowStatus => new NlpLookupTableDto
                {
                    Id = workflowStatus.Id.ToString(),
                    DisplayName = @L("StateTo") + workflowStatus.NlpWorkflowFk.Name + " : " + workflowStatus.StateName
                }).OrderBy(m => m.DisplayName).ToListAsync();

            list.Insert(0, new NlpLookupTableDto()
            {
                //Id = "00000000-0000-0000-0000-000000000000",
                Id = NlpWorkflowStateConsts.WfsNull.ToString(),
                DisplayName = L("SO_ExitWorkflow"),
            });

            list.Insert(0, new NlpLookupTableDto()
            {
                Id = NlpWorkflowStateConsts.WfsKeepCurrent.ToString(), //"00000000-0000-0000-0000-000000000010",
                DisplayName = L("CurrentWorkflowState"),
            });

            return list;
        }
    }
}
