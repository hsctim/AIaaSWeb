using System;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Controllers;

using Abp.Application.Services;
using Abp.Authorization;
using AIaaS.Nlp.Training;
using AIaaS.Nlp;
using System.Threading.Tasks;
using System.IO;
using AIaaS.Nlp.Dtos.NlpCbModel;
using ApiProtectorDotNet;
using Abp.Web.Models;
using Abp.UI;
using Abp.Auditing;

namespace AIaaS.Web.Areas.Api.Controllers
{
    [AbpAllowAnonymous]
    [Area("Api")]
    [DisableAuditing]
    public class ChatbotAIController : AIaaSControllerBase
    {
        public class RequestTrainingInput
        {
            public String SecuToken { set; get; }
        }


        private readonly INlpCbModelsAppService _nlpCbModelsAppServic;
        private readonly NlpTokenHelper _nlpTokenHelper;

        public ChatbotAIController(INlpCbModelsAppService nlpCbModelsAppServic,
             INlpChatbotsAppService nlpChatbotsAppService,
             NlpTokenHelper nlpTokenHelper)
        {
            _nlpCbModelsAppServic = nlpCbModelsAppServic;
            _nlpTokenHelper = nlpTokenHelper;
        }


        //static bool bEnableReturnDataForDebuggingPython = false;
        [HttpPost]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public async Task<NlpCbMTrainingDataDTO> RequestTraining([FromBody] RequestTrainingInput input)
        {
           if (_nlpTokenHelper.IsValid("NLP_TRAINING", input.SecuToken) == false)
                throw new UserFriendlyException(UserFriendlyExceptionCode.Code("AccessTokenError"), "L:AccessTokenError");

            var dto = await _nlpCbModelsAppServic.RequestTraining();

#if DEBUG
            //bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            //if (isDevelopment && (dto == null || dto.SourceData == null) && bEnableReturnDataForDebuggingPython)
            //    dto = await _nlpCbModelsAppServic.RequestTrainingTest();
#endif
            return dto;
        }



        [HttpPost]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public async Task<NlpCbMTrainingDataDTO> RequestTrainingForDebugging([FromBody] RequestTrainingInput input)
        {
            if (_nlpTokenHelper.IsValid("NLP_TRAINING", input.SecuToken) == false)
                throw new UserFriendlyException(UserFriendlyExceptionCode.Code("AccessTokenError"), "L:AccessTokenError");

            var dto = await _nlpCbModelsAppServic.RequestTraining();

            if (dto == null || dto.SourceData == null)
                dto = await _nlpCbModelsAppServic.RequestTrainingTest();
            return dto;
        }


        //[HttpPost]
        //public void CompleteTraining([FromBody] dynamic input)
        //{
        //    //_nlpCbModelsAppServic.CompleteTraining(input);
        //}



        //[ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public ActionResult CompleteTraining([FromBody] NlpCbMCompleteTrainingInputDto input)
        {
            if (_nlpTokenHelper.IsValid("NLP_TRAINING", input.SecuToken) == false)
                throw new UserFriendlyException(UserFriendlyExceptionCode.Code("AccessTokenError"), "L:AccessTokenError");

            try
            {
                _nlpCbModelsAppServic.CompleteTraining(input);
                return Content("OK");
            }
            catch (Exception e)
            {
                return Content(e.Message);
            }
        }

        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public ActionResult IncompleteTraining([FromBody] NlpCbMIncompleteTrainingInputDto input)
        {
            if (_nlpTokenHelper.IsValid("NLP_TRAINING", input.SecuToken) == false)
                throw new UserFriendlyException(UserFriendlyExceptionCode.Code("AccessTokenError"), "L:AccessTokenError");

            _nlpCbModelsAppServic.IncompleteTraining(input);
            return Content("OK");
        }


        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public async Task<ActionResult> IncompleteTrainingByPreemption([FromBody] NlpCbMIncompleteTrainingInputDto input)
        {
            if (_nlpTokenHelper.IsValid("NLP_TRAINING", input.SecuToken) == false)
                throw new UserFriendlyException(UserFriendlyExceptionCode.Code("AccessTokenError"), "L:AccessTokenError");

            await _nlpCbModelsAppServic.IncompleteTrainingByPreemption(input);
            return Content("OK");
        }


        /// <summary>
        /// 當Python Training程式啟動時，會通知MVC強制取消正在訓練中的模型
        /// 並清除快取
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public async Task<ActionResult> RestartAllOnTrainingModel([FromBody] NlpCbMIncompleteTrainingInputDto input)
        {
            if (_nlpTokenHelper.IsValid("NLP_TRAINING", input.SecuToken) == false)
                throw new UserFriendlyException(UserFriendlyExceptionCode.Code("AccessTokenError"), "L:AccessTokenError");

            await _nlpCbModelsAppServic.RestartAllOnTrainingModelAsync();
            return Content("OK");
        }
    }
}