using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.UI;
using Abp.Web.Models;
using Abp.Auditing;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos.NlpCbModel;
using AIaaS.Web.Controllers;
using AIaaS.Nlp.Training;

namespace AIaaS.Web.Areas.Api.Controllers
{
    /// <summary>
    /// ChatbotAIController
    /// Handles NLP model training and related operations for chatbots.
    /// </summary>
    [AbpAllowAnonymous]
    [Area("Api")]
    [DisableAuditing]
    public class ChatbotAIController : AIaaSControllerBase
    {
        // Input DTO for training requests
        public class RequestTrainingInput
        {
            public string SecuToken { get; set; }
        }

        private readonly INlpCbModelsAppService _nlpCbModelsAppService;
        private readonly NlpTokenHelper _nlpTokenHelper;

        /// <summary>
        /// Constructor for ChatbotAIController.
        /// </summary>
        /// <param name="nlpCbModelsAppService">Service for NLP model operations.</param>
        /// <param name="nlpTokenHelper">Helper for token validation.</param>
        public ChatbotAIController(
            INlpCbModelsAppService nlpCbModelsAppService,
            NlpTokenHelper nlpTokenHelper)
        {
            _nlpCbModelsAppService = nlpCbModelsAppService;
            _nlpTokenHelper = nlpTokenHelper;
        }

        /// <summary>
        /// Requests training for an NLP model.
        /// </summary>
        /// <param name="input">Training request input containing a security token.</param>
        /// <returns>Training data DTO.</returns>
        [HttpPost]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public async Task<NlpCbMTrainingDataDTO> RequestTraining([FromBody] RequestTrainingInput input)
        {
            ValidateToken(input.SecuToken, "NLP_TRAINING");

            return await _nlpCbModelsAppService.RequestTraining();
        }

        /// <summary>
        /// Requests training for debugging purposes.
        /// </summary>
        /// <param name="input">Training request input containing a security token.</param>
        /// <returns>Training data DTO.</returns>
        [HttpPost]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public async Task<NlpCbMTrainingDataDTO> RequestTrainingForDebugging([FromBody] RequestTrainingInput input)
        {
            ValidateToken(input.SecuToken, "NLP_TRAINING");

            var dto = await _nlpCbModelsAppService.RequestTraining();
            return dto ?? await _nlpCbModelsAppService.RequestTrainingTest();
        }

        /// <summary>
        /// Completes the training process for an NLP model.
        /// </summary>
        /// <param name="input">Input DTO containing training completion details.</param>
        /// <returns>Action result indicating success or failure.</returns>
        [HttpPost]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public ActionResult CompleteTraining([FromBody] NlpCbMCompleteTrainingInputDto input)
        {
            ValidateToken(input.SecuToken, "NLP_TRAINING");

            try
            {
                _nlpCbModelsAppService.CompleteTraining(input);
                return Content("OK");
            }
            catch (Exception e)
            {
                return Content(e.Message);
            }
        }

        /// <summary>
        /// Marks the training process as incomplete.
        /// </summary>
        /// <param name="input">Input DTO containing incomplete training details.</param>
        /// <returns>Action result indicating success.</returns>
        [HttpPost]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public ActionResult IncompleteTraining([FromBody] NlpCbMIncompleteTrainingInputDto input)
        {
            ValidateToken(input.SecuToken, "NLP_TRAINING");

            _nlpCbModelsAppService.IncompleteTraining(input);
            return Content("OK");
        }

        /// <summary>
        /// Marks the training process as incomplete due to preemption.
        /// </summary>
        /// <param name="input">Input DTO containing incomplete training details.</param>
        /// <returns>Action result indicating success.</returns>
        [HttpPost]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public async Task<ActionResult> IncompleteTrainingByPreemption([FromBody] NlpCbMIncompleteTrainingInputDto input)
        {
            ValidateToken(input.SecuToken, "NLP_TRAINING");

            await _nlpCbModelsAppService.IncompleteTrainingByPreemption(input);
            return Content("OK");
        }

        /// <summary>
        /// Restarts all models currently in training.
        /// </summary>
        /// <param name="input">Input DTO containing security token.</param>
        /// <returns>Action result indicating success.</returns>
        [HttpPost]
        [WrapResult(WrapOnSuccess = false, WrapOnError = false)]
        public async Task<ActionResult> RestartAllOnTrainingModel([FromBody] NlpCbMIncompleteTrainingInputDto input)
        {
            ValidateToken(input.SecuToken, "NLP_TRAINING");

            await _nlpCbModelsAppService.RestartAllOnTrainingModelAsync();
            return Content("OK");
        }

        /// <summary>
        /// Validates the provided security token.
        /// </summary>
        /// <param name="token">The security token to validate.</param>
        /// <param name="tokenType">The expected token type.</param>
        private void ValidateToken(string token, string tokenType)
        {
            if (!_nlpTokenHelper.IsValid(tokenType, token))
            {
                throw new UserFriendlyException(UserFriendlyExceptionCode.Code("AccessTokenError"), "L:AccessTokenError");
            }
        }
    }
}
