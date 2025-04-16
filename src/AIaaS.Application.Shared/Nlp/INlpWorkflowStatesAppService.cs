using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Collections.Generic;

namespace AIaaS.Nlp
{
    public interface INlpWorkflowStatesAppService : IApplicationService
    {
        Task<PagedResultDto<GetNlpWorkflowStateForViewDto>> GetAll(GetAllNlpWorkflowStatesInput input);

        //Task<GetNlpWorkflowStateForViewDto> GetNlpWorkflowStateForView(Guid id);

        Task<GetNlpWorkflowStateForEditOutput> GetNlpWorkflowStateForEdit(EntityDto<Guid> input);

        Task CreateOrEdit(CreateOrEditNlpWorkflowStateDto input);

        Task Delete(EntityDto<Guid> input);

        //Task<List<NlpWorkflowStateNlpWorkflowLookupTableDto>> GetAllNlpWorkflowForTableDropdown();

        Task<List<NlpLookupTableDto>> GetAllNlpWorkflowStateForTableDropdown();

    }
}