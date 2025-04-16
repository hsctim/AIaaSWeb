using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;

namespace AIaaS.Nlp
{
    public interface INlpTokensAppService : IApplicationService
    {
        Task<PagedResultDto<GetNlpTokenForViewDto>> GetAll(GetAllNlpTokensInput input);

        GetNlpTokenForEditOutput GetNlpTokenForEdit(EntityDto<Guid> input);

        void CreateOrEdit(CreateOrEditNlpTokenDto input);

        void Delete(EntityDto<Guid> input);
    }
}