using Abp.Application.Services;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using Abp.Runtime.Session;
using AIaaS.Helpers;
using AIaaS.Nlp.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp
{

    [RemoteService(false)]
    public class NlpChatbotFunction : AIaaSAppServiceBase, ITransientDependency
    {
        private readonly IRepository<NlpChatbot, Guid> _nlpChatbotRepository;
        private readonly ICacheManager _cacheManager;
        private NlpChatbotDto __nlpChatbotCache;

        public NlpChatbotFunction(ICacheManager cacheManager, IRepository<NlpChatbot, Guid> nlpChatbotRepository)
        {
            _cacheManager = cacheManager;
            _nlpChatbotRepository = nlpChatbotRepository;
        }

        public NlpChatbotDto GetChatbotDto(Guid chatbotId)
        {
            if (__nlpChatbotCache != null && __nlpChatbotCache.Id == chatbotId)
                return __nlpChatbotCache;

            __nlpChatbotCache = (NlpChatbotDto)_cacheManager.Get_NlpChatbotDto(chatbotId);

            if (__nlpChatbotCache != null)
                return __nlpChatbotCache;

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MustHaveTenant, AbpDataFilters.MayHaveTenant))
            {
                __nlpChatbotCache = ObjectMapper.Map<NlpChatbotDto>(
                    _nlpChatbotRepository.FirstOrDefault(c => c.Id == chatbotId && c.Disabled == false && c.IsDeleted == false));

                if (__nlpChatbotCache == null)
                    __nlpChatbotCache = new NlpChatbotDto()
                    {
                        Id = chatbotId,
                        IsDeleted = true,
                        Disabled = true
                    };

                if (__nlpChatbotCache.PredThreshold <= 0f || __nlpChatbotCache.PredThreshold >= 1f)
                    __nlpChatbotCache.PredThreshold = NlpChatbotConsts.DefaultPredThreshold;

                if (__nlpChatbotCache.WSPredThreshold <= 0f || __nlpChatbotCache.WSPredThreshold >= 1f)
                    __nlpChatbotCache.WSPredThreshold = NlpChatbotConsts.DefaultWSPredThreshold;

                if (__nlpChatbotCache.SuggestionThreshold <= 0f || __nlpChatbotCache.SuggestionThreshold >= 1f)
                    __nlpChatbotCache.SuggestionThreshold = NlpChatbotConsts.DefaultSuggestionThreshold;

                _cacheManager.Set_NlpChatbotDto(chatbotId, __nlpChatbotCache);
                return __nlpChatbotCache;
            }
        }

        public string GetChatbotLanguage(Guid chatbotId)
        {
            return GetChatbotDto(chatbotId)?.Language;
        }

        public Guid? GetChatbotPictureId(Guid chatbotId)
        {
            return GetChatbotDto(chatbotId)?.ChatbotPictureId;
        }

        public string GetChatbotName(Guid chatbotId)
        {
            return GetChatbotDto(chatbotId)?.Name;
        }

        public bool IsWebAPIEnabled(Guid chatbotId)
        {
            var chatbot = GetChatbotDto(chatbotId);
            if (chatbot != null && chatbot.EnableWebAPI)
                return true;
            else
                return false;
        }


        public bool IsFacebookEnabled(Guid chatbotId)
        {
            var chatbot = GetChatbotDto(chatbotId);
            if (chatbot != null && chatbot.EnableFacebook)
                return true;
            else
                return false;
        }


        public bool IsLineEnabled(Guid chatbotId)
        {
            var chatbot = GetChatbotDto(chatbotId);
            if (chatbot != null && chatbot.EnableLine)
                return true;
            else
                return false;
        }


        public bool IsSignalREnabled(Guid chatbotId)
        {
            var chatbot = GetChatbotDto(chatbotId);
            if (chatbot != null && chatbot.EnableWebChat)
                return true;
            else
                return false;
        }

        public int GetTenantId(Guid chatbotId)
        {
            return GetChatbotDto(chatbotId).TenantId;
        }


        public IRepository<NlpChatbot, Guid> GetRepository()
        {
            return _nlpChatbotRepository;
        }

        public void DeleteCache(Guid chatbotId)
        {
            _cacheManager.Remove_NlpChatbotDto(chatbotId);
        }
    }
}
