using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Collections.Generic;

namespace AIaaS.Nlp
{
    public interface INlpWorkflowsAppService : IApplicationService
    {
        Task<PagedResultDto<GetNlpWorkflowForViewDto>> GetAll(GetAllNlpWorkflowsInput input);

        //Task<GetNlpWorkflowForViewDto> GetNlpWorkflowForView(Guid id);

        Task<GetNlpWorkflowForEditOutput> GetNlpWorkflowForEdit(EntityDto<Guid> input);

        Task CreateOrEdit(CreateOrEditNlpWorkflowDto input);

        Task Delete(EntityDto<Guid> input);

        Task<List<NlpLookupTableDto>> GetAllNlpChatbotForTableDropdown();


        //Task<List<NlpWorkflowDto>> GetAllForSelectList(Guid? chatbotId);

        Task<NlpWorkflowChatbotDto> GetNlpWorkflowDto(Guid id);

    }
}