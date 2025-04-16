using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Collections.Generic;
using AIaaS.Nlp.Dtos.NlpCbTrainedAnswer;

namespace AIaaS.Nlp
{
    public interface INlpCbTrainedAnswersAppService : IApplicationService
    {
        //Task<PagedResultDto<GetNlpCbTrainedAnswerForViewDto>> GetAll(GetAllNlpCbTrainedAnswersInput input);

        //Task<GetNlpCbTrainedAnswerForEditOutput> GetNlpCbTrainedAnswerForEdit(EntityDto<Guid> input);

        //Task CreateOrEdit(CreateOrEditNlpCbTrainedAnswerDto input);

        //Task Delete(EntityDto<Guid> input);

        //Task<List<NlpCbTrainedAnswerNlpCbTrainingDataLookupTableDto>> GetAllNlpCbTrainingDataForTableDropdown();

        void Create(NlpCbTAChatbotTrainingInsertDto input);

        Task DeleteUnusedData();
        void Delete(Guid trainingDataId);
    }
}