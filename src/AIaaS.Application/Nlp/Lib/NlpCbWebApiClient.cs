using Abp.Application.Services;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Runtime.Caching;
using Abp.Threading;
using Abp.UI;
using AIaaS.Configuration;
using AIaaS.Helper;
using AIaaS.Helpers;
using AIaaS.Nlp.Dtos;
using AIaaS.Nlp.Lib.Dtos;
using Castle.Core.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace AIaaS.Nlp.Lib
{
    [RemoteService(false)]
    public class NlpCbWebApiClient : ITransientDependency
    {
        private readonly ICacheManager _cacheManager;
        //private readonly ILogger Logger;

        private static string _url;
        private static string _nlpChatbotRequestTrainingURL;
        private static string _nlpChatbotCancelTrainingURL;
        private static string _nlpChatbotGetTrainingStatusURL;
        private static string _nlpChatbotPredictURL;
        private static string _nlpChatbotDeleteNlpModelURL;
        private static string _nlpChatbotSimilarityURL;

        private string SECU_TOKEN = "NLP_PYTHON_SERVICE";
        private readonly NlpTokenHelper _nlpTokenHelper;
        private string _secuToken;

        public NlpCbWebApiClient(
            ICacheManager cacheManager,
            //ILogger logger,
            NlpTokenHelper nlpTokenHelper,
            IAppConfigurationAccessor configurationAccessor
            )
        {
            _cacheManager = cacheManager;
            //Logger = logger;
            _nlpTokenHelper = nlpTokenHelper;

            if (string.IsNullOrEmpty(_url))
            {
                _url = configurationAccessor.Configuration["NlpChatbot:PythonChatbotApiURL"];

                _nlpChatbotRequestTrainingURL = _url + "/RequestTraining";
                _nlpChatbotCancelTrainingURL = _url + "/CancelTraining";
                _nlpChatbotGetTrainingStatusURL = _url + "/GetTrainingStatus";
                _nlpChatbotPredictURL = _url + "/ChatbotPredict";
                _nlpChatbotDeleteNlpModelURL = _url + "/DeleteNlpModel";
                _nlpChatbotSimilarityURL = _url + "/Similarity";
            }
        }


        public void RequestTrainingAsync(bool freeEdition)
        {
            _secuToken ??= _nlpTokenHelper.GetTokenValue(SECU_TOKEN);

            var input = new NlpCbRequestTrainingInput()
            {
                secuToken = _secuToken,
                freeEdition = freeEdition
            };

            _ = Http.HttpRequest.PostObjectAsync<NlpCbRequestTrainingResult, NlpCbRequestTrainingInput>(_nlpChatbotRequestTrainingURL, input);
        }


        public async Task<NlpCbRequestTrainingResult> CancelTrainingAsync(Guid chatbotId)
        {
            _secuToken ??= _nlpTokenHelper.GetTokenValue(SECU_TOKEN);

            var input = new NlpCbCancelTrainingInput()
            {
                secuToken = _secuToken,
                chatbotId = chatbotId
            };

            var result = await Http.HttpRequest.PostObjectAsync<NlpCbRequestTrainingResult, NlpCbCancelTrainingInput>(_nlpChatbotCancelTrainingURL, input);

            return result;
        }


        public async Task<NlpCbGetTrainingStatus> GetTrainingStatusAsync()
        {
            _secuToken ??= _nlpTokenHelper.GetTokenValue(SECU_TOKEN);

            var input = new NlpCbRequestTrainingInput()
            {
                secuToken = _secuToken,
            };

            var result = await Http.HttpRequest.PostObjectAsync<NlpCbGetTrainingStatus, NlpCbRequestTrainingInput>(_nlpChatbotGetTrainingStatusURL, input);

            return result;
        }


        public async Task DeleteTrainingModelAsync(Guid chatbotId)
        {
            _secuToken ??= _nlpTokenHelper.GetTokenValue(SECU_TOKEN);

            var input = new NlpCbDeleteTrainingModelInput()
            {
                secuToken = _secuToken,
                chatbotId = chatbotId
            };

            await Http.HttpRequest.PostObjectAsync<NlpCbDeleteTrainingModelResult, NlpCbDeleteTrainingModelInput>(_nlpChatbotDeleteNlpModelURL, input);
        }


        public async Task<NlpCbGetChatbotPredictResult> ChatbotPredict(Guid chatbotId, string question, Guid? workflowState)
        {
            if (workflowState == null)
                workflowState = Guid.Empty;

            string key = ToSHA256(question, workflowState);
            var predictResult = (NlpCbGetChatbotPredictResult)_cacheManager.Get_ChatbotPredict(chatbotId, key);
            // predictResult = null;
            if (predictResult != null)
                return predictResult;

            _secuToken ??= _nlpTokenHelper.GetTokenValue(SECU_TOKEN);

            NlpCbGetChatbotPredictResult result = await
            Http.HttpRequest.PostObjectAsync<NlpCbGetChatbotPredictResult, NlpCbGetChatbotPredictInput>(_nlpChatbotPredictURL, new NlpCbGetChatbotPredictInput()
            {
                chatbotId = chatbotId,
                question = question,
                state = workflowState?.ToString(),
                //language = language,
                secuToken = _secuToken
            });

            _cacheManager.Set_ChatbotPredict(chatbotId, key, result);
            return result;
        }


        public async Task<GetSentencesSimilarityResult> GetSentencesSimilarityAsync(
            string src, IList<IList<string>> refs)
        {
            _secuToken ??= _nlpTokenHelper.GetTokenValue(SECU_TOKEN);

            var input = new GetSentencesSimilarityInput()
            {
                src = src,
                refs = refs,
                secuToken = _secuToken,
            };

            var result = new GetSentencesSimilarityResult()
            {
                similarities = new List<float>(),
                errorCode = "success"
            };

            foreach (var questions in refs)
            {
                float? data = _cacheManager.Get_SentenceSimiliarity(src, questions);
                if (data == null)
                {
                    result = null;
                    break;
                }

                result.similarities.Add((float)data);
            }

            if (result != null)
                return result;

            result = await Http.HttpRequest.PostObjectAsync<GetSentencesSimilarityResult, GetSentencesSimilarityInput>(_nlpChatbotSimilarityURL, input);

            var i = 0;
            var sArray = result.similarities.ToArray();
            foreach (var questions in refs)
            {
                _cacheManager.Set_SentenceSimiliarity(src, questions, sArray[i]);
                i++;
            }

            return result;
        }

        public string ToSHA256(string question, Guid? workflowState)
        {
            if (!workflowState.HasValue)
                workflowState = Guid.Empty;

            var wfsStr = workflowState.ToString();

            using (var cryptoSHA256 = SHA256.Create())
            {
                IEnumerable<byte> bytes = Encoding.UTF8.GetBytes(wfsStr + ":" + question);

                var hash = cryptoSHA256.ComputeHash(bytes.ToArray());
                //取得 sha256
                var sha256 = BitConverter.ToString(hash).Replace("-", String.Empty);
                return sha256;
            }
        }

    }
}
