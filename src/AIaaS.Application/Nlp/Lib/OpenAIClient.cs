using Abp.Application.Services;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Runtime.Caching;
using Abp.Timing;
using AIaaS.Configuration;
using AIaaS.Helpers;
using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib
{
    public class OpenAIResult
    { 
        public string text { get; set; }
        public int cost { get; set; }
    }


    [RemoteService(false)]
    public class OpenAIClient : ITransientDependency
    {
        public ILogger Logger { protected get; set; }
        private readonly ICacheManager _cacheManager;
        private readonly NlpChatbotFunction _nlpChatbotFunction;

        private readonly IAppConfigurationAccessor _configurationAccessor;

        static private OpenAiConfiguration _openAiConfiguration = null;

        private readonly IRepository<ChatGptText, Guid> _chatGptTextRepository;


        public OpenAIClient(ICacheManager cacheManager,
            NlpChatbotFunction nlpChatbotFunction,
            IAppConfigurationAccessor configurationAccessor,
            IRepository<ChatGptText, Guid> chatGptTextRepository

            )
        {
            Logger = NullLogger.Instance;
            _cacheManager = cacheManager;
            _nlpChatbotFunction = nlpChatbotFunction;
            _configurationAccessor = configurationAccessor;
            _chatGptTextRepository = chatGptTextRepository;
        }

        public OpenAiConfiguration GetDefaultAiConfiguration()
        {
            var dto = new OpenAiConfiguration()
            {
                IsEnabled = bool.Parse(_configurationAccessor.Configuration["OpenAI:IsEnabled"]),
                ApiKey = _configurationAccessor.Configuration["OpenAI:ApiKey"]?.Trim(),
                Engine = _configurationAccessor.Configuration["OpenAI:Engine"]?.Trim(),
                Temperature = float.Parse(_configurationAccessor.Configuration["OpenAI:Temperature"]),
                MaxTokens = int.Parse(_configurationAccessor.Configuration["OpenAI:MaxTokens"]),
                Organization = _configurationAccessor.Configuration["OpenAI:Organization"]?.Trim(),
                MessagesTemplate = _configurationAccessor.Configuration["OpenAI:MessagesTemplate"]?.Trim(),
            };

            return dto;
        }


        private static string ToSHA256(string apiKey, string text)
        {
            using (var cryptoSHA256 = SHA256.Create())
            {
                IEnumerable<byte> bytes = Encoding.UTF8.GetBytes(apiKey +":"+ text);

                var hash = cryptoSHA256.ComputeHash(bytes.ToArray());

                var sha256 = BitConverter.ToString(hash).Replace("-", String.Empty);
                return sha256;
            }
        }

        public async Task<OpenAIResult> Chat(Guid chatbotId, string question)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);

            if (chatbot == null || chatbot.EnableOPENAI == (int)NlpChatbotConsts.EnableGPTType.Disabled)
                return null;

            OpenAiConfiguration openAiConfiguration = null;

            if (chatbot.EnableOPENAI == (int)NlpChatbotConsts.EnableGPTType.UsingSystem )
            {
                _openAiConfiguration ??= GetDefaultAiConfiguration();
                openAiConfiguration = _openAiConfiguration;
                chatbot.OPENAIKey = openAiConfiguration.ApiKey;
                chatbot.OPENAICache = true;
            }

            if (chatbot.EnableOPENAI == (int)NlpChatbotConsts.EnableGPTType.UsingPrivate)
            {
                openAiConfiguration = new OpenAiConfiguration()
                {
                    IsEnabled = true,
                    ApiKey = chatbot.OPENAIKey,
                    Organization = _openAiConfiguration.Organization,
                    Engine = _openAiConfiguration.Engine,
                    MaxTokens = _openAiConfiguration.MaxTokens,
                    Temperature = _openAiConfiguration.Temperature,
                    MessagesTemplate = _openAiConfiguration.MessagesTemplate,
                };
            }

            var sha256 = ToSHA256(chatbot.OPENAIKey?.Trim(), question?.Trim());
            var dt = Clock.Now;

            if (chatbot.OPENAICache == true || chatbot.EnableOPENAI == (int)NlpChatbotConsts.EnableGPTType.UsingSystem)
            {
                try
                {
                    var answer = (OpenAIResult)_cacheManager.Get_GPTCache(sha256);

                    if (answer != null)
                    {
                        var ret = new OpenAIResult()
                        {
                            text = answer.text,
                            cost = answer.cost / 10
                        };

                        if (chatbot.EnableOPENAI == (int)NlpChatbotConsts.EnableGPTType.UsingPrivate)
                            ret.cost /= 10;

                        ret.cost = Math.Max(ret.cost, 1);
                        return ret;
                    }

                    var chatGptText = await _chatGptTextRepository.GetAll()
                        .Where(e => e.Key == sha256 && e.CreationTime > dt.AddMonths(-1))
                        .OrderByDescending(e => e.CreationTime)
                        .FirstAsync();

                    if (chatGptText != null)
                    {
                        answer = JsonConvert.DeserializeObject<OpenAIResult>(chatGptText.Text);
                        _cacheManager.Set_GPTCache(sha256, answer);
                        return new OpenAIResult()
                        {
                            text = answer.text,
                            cost = answer.cost / 10
                        };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.ToString(), ex);
                }
            }            

            //get generative text from openAI
            OpenAIAPI api = null;
            if (string.IsNullOrEmpty(openAiConfiguration.Organization))
                api = new OpenAIAPI(openAiConfiguration.ApiKey);
            else
                api = new OpenAIAPI(new APIAuthentication(openAiConfiguration.ApiKey, openAiConfiguration.Organization));

            var questionGPTMsgs = DeserializeMessage(openAiConfiguration.MessagesTemplate, question);

            ChatResult chatResult = null;

            for (int n = 0; n < 3; n++)
            {
                try
                {
                    chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
                    {
                        Model = openAiConfiguration.Engine,
                        Temperature = openAiConfiguration.Temperature,
                        MaxTokens = openAiConfiguration.MaxTokens,
                        Messages = questionGPTMsgs,
                    });

                    break;
                }
                catch (Exception ex)
                {
                    await Task.Delay(1000);
                    Logger.Fatal(ex.ToString(), ex);

                    if (n == 2)
                        throw;
                }                
            }


            var result = new OpenAIResult()
            {
                text = chatResult.ToString(),
                cost = chatResult.Usage.TotalTokens / 10,
            };

            if (chatbot.OPENAICache == true || chatbot.EnableOPENAI == (int)NlpChatbotConsts.EnableGPTType.UsingSystem)
            {
                _cacheManager.Set_GPTCache(sha256, result);

                //json serialize
                var jsonStr = JsonConvert.SerializeObject(result);
                await _chatGptTextRepository.InsertAsync(new ChatGptText()
                {
                    Key = sha256,
                    Text = jsonStr,
                    CreationTime = Clock.Now
                });
            }

            return result;
        }
    
    
        public IList<ChatMessage> DeserializeMessage(string jsonStr, string question)
        {
            try
            {
                var messageList = JsonConvert.DeserializeObject< IList<ChatMessage>>(jsonStr);

                foreach (var item in messageList)
                    item.Content=item.Content.Replace("[question]", question, StringComparison.OrdinalIgnoreCase);

                return messageList;

            }
            catch (Exception)
            {
                var list = new List<ChatMessage>
                {
                    new ChatMessage()
                    {
                        Content = question,
                        Role = ChatMessageRole.User
                    }
                };

                return list;
            }
        }
    }
}
