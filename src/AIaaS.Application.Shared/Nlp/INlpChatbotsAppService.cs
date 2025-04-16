using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Linq;
using AIaaS.Nlp.Dtos.NlpCbModel;
using System.Collections.Generic;
using AIaaS.License;
using AIaaS.Nlp.Lib.Dtos;

namespace AIaaS.Nlp
{
    public interface INlpChatbotsAppService : IApplicationService
    {
        Task<PagedResultDto<GetNlpChatbotForViewDto>> GetAll(GetAllNlpChatbotsInput input);

        Task<IList<ChatbotTrainingStatusForListView>> GetAllTrainingStatus();

        //GetNlpChatbotSelectOptionsDto GetSelectOptions(String language);

        Task<GetNlpChatbotForEditOutput> GetNlpChatbotForEdit(EntityDto<Guid> input);

        Task<NlpChatbotDto> CreateOrEdit(CreateOrEditNlpChatbotDto input);

        Task Delete(EntityDto<Guid> input);

        Task TrainChatbot(Guid chatbotId, bool rebuild);

        Task StopTrainingChatbot(Guid chatbotId);

        Task<NlpChatbotTrainingStatus> GetChatbotTrainingStatus(Guid chatbotId);

        Task<List<NlpChatbotDto>> GetAllForSelectList();


        //Task<string>GetChatbotLanguage(Guid chatbotId);

        Task<byte[]> GetProfilePicture(Guid pictureId);

        Task<Guid?> DeleteProfilePicture(Guid chatbotId);

        List<string> GetDefaultProfilePictures();

        //string GetChatbotJson(Guid chatbotId);
        Task<FileDto> GetNlpChatbotsToFile(Guid chatbotId);

        Task ImportJsonFile(string jsonData);

        Task<int> GetChatbotCount();

        Task<LicenseUsage> GetChatbotLicenseUsage();

        Task CreateChatbotSampleAsync(int tenantId, long? userId);

    }
}