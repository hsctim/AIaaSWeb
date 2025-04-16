using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Collections.Generic;

namespace AIaaS.Nlp
{
    public interface INlpCbQAAccuraciesAppService : IApplicationService
    {
        Task<PagedResultDto<GetNlpCbQAAccuracyForViewDto>> GetAll(GetAllNlpCbQAAccuraciesInput input);

        //Task<GetNlpCbQAAccuracyForEditOutput> GetNlpCbQAAccuracyForEdit(EntityDto<Guid> input);

        //Task CreateOrEdit(CreateOrEditNlpCbQAAccuracyDto input);


    }
}