using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Collections.Generic;
using AIaaS.Nlp.Training;

namespace AIaaS.Nlp
{
    public interface INlpCbTrainingDatasAppService : IApplicationService
    {
        //Task<PagedResultDto<GetNlpCbTrainingDataForViewDto>> GetAll(GetAllNlpCbTrainingDatasInput input);

        //Task<GetNlpCbTrainingDataForEditOutput> GetNlpCbTrainingDataForEdit(EntityDto<Guid> input);

        //Task CreateOrEdit(CreateOrEditNlpCbTrainingDataDto input);

        //Task Delete(EntityDto<Guid> input);

        //Task<List<NlpCbTrainingDataNlpChatbotLookupTableDto>> GetAllNlpChatbotForTableDropdown();

        Task<Guid> CreateNewTrainingDataAsync(Guid chatbotId, NlpCbMSourceData nlpCbMRawData);
    }
}