using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Dto;
using AIaaS.License;
using AIaaS.Nlp.Dtos;
using AIaaS.Nlp.Dtos.NlpQA;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIaaS.Nlp
{
    public interface INlpQAsAppService : IApplicationService
    {
        Task<PagedResultDto<GetNlpQAForViewDto>> GetAll(GetAllNlpQAsInput input);

        Task<GetNlpQAForEditOutput> GetNlpQAForEdit(EntityDto<Guid> input);

        Task<GetNlpQAForEditOutput> DiscardNlpQAForEditAsync(EntityDto<Guid> chatbotId);

        Task CreateOrEdit(CreateOrEditNlpQADto input);

        void Delete(EntityDto<Guid> input);

        //Task ImportJsonFile(Guid chatbotId, byte[] excelData);

        //Task<QueryQuestionSegmentsOutput> QueryQuestionSegments(QueryQuestionSegmentsInput input);

        void CreaetQAForAccuracy(CreaetQAForAccuracyDto input);

        //void CheckNNID0(Guid chatbotId);
        Task CheckNNID0Async(Guid chatbotId);

        Task<FileDto> GetNlpQAsToFile(Guid chatbotId);

        Task<GetCaterogiesOutput> GetCaterogies(Guid chatbotId);
        //Task<bool> IsExceedQuestionLimitation();
        Task<LicenseUsage> GetQuestionLicenseUsage(Guid chatbotId);

        Task<List<NlpWorkflowStateSelection>> GetAllNlpWorkflowStateForTableDropdown(Guid chatbotId);

        Task<List<NlpLookupTableDto>> GetAllNlpChatbotForTableDropdown();

        Task DeleteSelections(DeleteSelectionInput input);

        Task<int> GetQaCount(Guid chatbotId);

    }
}