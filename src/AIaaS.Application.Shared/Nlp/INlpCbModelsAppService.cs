using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Collections.Generic;
using AIaaS.Nlp.Training;
using AIaaS.Nlp.Dtos.NlpCbModel;
using AIaaS.Nlp.Lib.Dtos;

namespace AIaaS.Nlp
{
    public interface INlpCbModelsAppService : IApplicationService
    {
        Task<PagedResultDto<GetNlpCbModelForViewDto>> GetAll(GetAllNlpCbModelsInput input);

        //GetNlpCbModelForEditOutput GetNlpCbModelForEdit(EntityDto<Guid> input);


        Task Create(CreateOrEditNlpCbModelDto input);


        Task DeleteQueueingModel(Guid chatbotId);

        Task CancelChatbotTrainingActivities(Guid chatbotId);

        Task<NlpChatbotTrainingStatus> GetChatbotTrainingStatus(Guid chatbotId, bool reEnter = false);

        Task<NlpCbMTrainingDataDTO> RequestTraining();



        Task<NlpCbMTrainingDataDTO> RequestTrainingTest();

        //void KeepaliveTraining(String secToken, Guid chatbotId);

        void CompleteTraining(NlpCbMCompleteTrainingInputDto input);

        void IncompleteTraining(NlpCbMIncompleteTrainingInputDto input);


        Task IncompleteTrainingByPreemption(NlpCbMIncompleteTrainingInputDto input);

        
        Task RestartAllOnTrainingModelAsync(bool cancelModel = false);
    }
}