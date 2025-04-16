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
using Abp.Auditing;
using Abp.Application.Services;
using AIaaS.Nlp.Lib;
using Newtonsoft.Json;
using Abp.Runtime.Caching;
using AIaaS.Helpers;


namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows)]
    public class NlpWorkflowsAppService : AIaaSAppServiceBase, INlpWorkflowsAppService
    {
        private readonly IRepository<NlpWorkflow, Guid> _nlpWorkflowRepository;
        private readonly IRepository<NlpChatbot, Guid> _lookup_nlpChatbotRepository;
        private readonly NlpCbSession _nlpCbSession;
        private readonly ICacheManager _cacheManager;

        public NlpWorkflowsAppService(IRepository<NlpWorkflow, Guid> nlpWorkflowRepository, IRepository<NlpChatbot, Guid> lookup_nlpChatbotRepository,
            NlpCbSession nlpCbSession, ICacheManager cacheManager)
        {
            _nlpWorkflowRepository = nlpWorkflowRepository;
            _lookup_nlpChatbotRepository = lookup_nlpChatbotRepository;
            _nlpCbSession = nlpCbSession;
            _cacheManager = cacheManager;
        }

        public async Task<PagedResultDto<GetNlpWorkflowForViewDto>> GetAll(GetAllNlpWorkflowsInput input)
        {
            var filteredNlpWorkflows = _nlpWorkflowRepository.GetAll()
                        .Include(e => e.NlpChatbotFk)
                        .Where(e => e.NlpChatbotFk != null && e.NlpChatbotFk.Disabled == false)
                        .WhereIf(input.NlpChatbotId != null, e => input.NlpChatbotId == e.NlpChatbotId);

            var pagedAndFilteredNlpWorkflows = filteredNlpWorkflows
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var nlpWorkflows = from o in pagedAndFilteredNlpWorkflows
                               join o1 in _lookup_nlpChatbotRepository.GetAll() on o.NlpChatbotId equals o1.Id into j1
                               from s1 in j1.DefaultIfEmpty()

                               select new
                               {

                                   o.Name,
                                   o.Disabled,
                                   Id = o.Id,
                                   NlpChatbotName = s1.Name.ToString()
                               };

            var totalCount = await filteredNlpWorkflows.CountAsync();

            var dbList = await nlpWorkflows.ToListAsync();
            var results = new List<GetNlpWorkflowForViewDto>();

            foreach (var o in dbList)
            {
                var res = new GetNlpWorkflowForViewDto()
                {
                    NlpWorkflow = new NlpWorkflowDto
                    {

                        Name = o.Name,
                        Disabled = o.Disabled,
                        Id = o.Id,
                    },
                    NlpChatbotName = o.NlpChatbotName
                };

                results.Add(res);
            }

            _nlpCbSession["ChatbotId"] = input.NlpChatbotId?.ToString();

            return new PagedResultDto<GetNlpWorkflowForViewDto>(
                totalCount,
                results
            );

        }


        public async Task<GetNlpWorkflowForEditOutput> GetNlpWorkflowForEdit(EntityDto<Guid> input)
        {
            var nlpWorkflow = await _nlpWorkflowRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetNlpWorkflowForEditOutput { NlpWorkflow = ObjectMapper.Map<CreateOrEditNlpWorkflowDto>(nlpWorkflow) };

            return output;
        }

        public async Task CreateOrEdit(CreateOrEditNlpWorkflowDto input)
        {
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
        protected virtual async Task Create(CreateOrEditNlpWorkflowDto input)
        {
            var nlpWorkflow = ObjectMapper.Map<NlpWorkflow>(input);
            nlpWorkflow.Vector = JsonConvert.SerializeObject(NlpHelper.CreateNewVector());

            if (AbpSession.TenantId != null)
            {
                nlpWorkflow.TenantId = (int)AbpSession.TenantId;
            }

            await _nlpWorkflowRepository.InsertAsync(nlpWorkflow);

            _cacheManager.Remove_NlpCbDictWofkflowState(nlpWorkflow.NlpChatbotId);

        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Edit)]
        protected virtual async Task Update(CreateOrEditNlpWorkflowDto input)
        {
            var nlpWorkflow = await _nlpWorkflowRepository.FirstOrDefaultAsync((Guid)input.Id);
            int[] vector;
            try
            {
                vector = JsonConvert.DeserializeObject<int[]>(nlpWorkflow.Vector);
                if (vector.Length != 300)
                    throw new Exception();
            }
            catch (Exception)
            {
                vector = NlpHelper.CreateNewVector();
            }
            ObjectMapper.Map(input, nlpWorkflow);
            nlpWorkflow.Vector = JsonConvert.SerializeObject(vector);

            _cacheManager.Remove_NlpCbDictWofkflowState(nlpWorkflow.NlpChatbotId);
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Delete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            await _nlpWorkflowRepository.DeleteAsync(input.Id);
        }

        [RemoteService(false)]
        public async Task<List<NlpLookupTableDto>> GetAllNlpChatbotForTableDropdown()
        {
            return await _lookup_nlpChatbotRepository.GetAll()
                .Select(nlpChatbot => new NlpLookupTableDto
                {
                    Id = nlpChatbot.Id.ToString(),
                    DisplayName = nlpChatbot == null || nlpChatbot.Name == null ? "" : nlpChatbot.Name.ToString()
                }).ToListAsync();
        }

        //[RemoteService(false)]
        //[DisableAuditing]
        //public async Task<List<NlpWorkflowDto>> GetAllForSelectList(Guid? chatbotId)
        //{
        //    var filteredNlpWorkflows = _nlpWorkflowRepository.GetAll()
        //    .Include(e => e.NlpChatbotFk)
        //    .WhereIf(chatbotId != null, e => e.NlpChatbotId == chatbotId)
        //    .Where(e => e.NlpChatbotFk == null || e.NlpChatbotFk.Disabled == false)
        //    .Where(e => e.Disabled == false);

        //    var workflows = await (from o in filteredNlpWorkflows
        //                           where o.TenantId == AbpSession.TenantId
        //                           select new NlpWorkflowDto
        //                           {
        //                               Id = o.Id,
        //                               Name = o.Name,
        //                               Disabled = o.Disabled,
        //                           }).ToListAsync();

        //    return workflows;
        //}

        [RemoteService(false)]
        [DisableAuditing]
        public async Task<NlpWorkflowChatbotDto> GetNlpWorkflowDto(Guid id)
        {
            if (id == Guid.Empty)
            {
                var nlpWorkflows = from o in _nlpWorkflowRepository.GetAll()
                                   .Include(e => e.NlpChatbotFk)
                                   .OrderBy(e=>e.CreationTime)
                                   select new NlpWorkflowChatbotDto
                                   {
                                       Name = o.Name,
                                       Id = o.Id,
                                       NlpChatbotId = o.NlpChatbotId,
                                       ChatbotName = o.NlpChatbotFk.Name
                                   };

                return await nlpWorkflows.FirstAsync();
            }
            else
            {
                var nlpWorkflows = from o in _nlpWorkflowRepository.GetAll()
                                   .Include(e => e.NlpChatbotFk)
                                   .Where(e => e.Id == id)
                                   select new NlpWorkflowChatbotDto
                                   {
                                       Name = o.Name,
                                       Id = o.Id,
                                       NlpChatbotId = o.NlpChatbotId,
                                       ChatbotName = o.NlpChatbotFk.Name
                                   };

                return await nlpWorkflows.FirstAsync();
            }
        }
    }
}
