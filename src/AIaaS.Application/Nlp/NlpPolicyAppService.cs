using Abp.Application.Features;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Runtime.Caching;
using Abp.UI;
using AIaaS.Features;
using System;
using System.Linq;
using System.Threading.Tasks;
using AIaaS.Helpers;
using AIaaS.Nlp.Cache;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using AIaaS.Editions;
using AIaaS.MultiTenancy;
using System.Collections.Concurrent;
using System.Threading;
using AIaaS.Nlp.Dto;
using Abp.Auditing;
using Abp.EntityFrameworkCore.EFPlus;
using Abp.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIaaS.Nlp
{
    public class NlpPolicyAppService : AIaaSServiceBase, INlpPolicyAppService
    {
        private class NlpTenant : Tenant //Entity<int>
        {
        }

        private readonly IFeatureChecker _featureChecker;
        private readonly IRepository<NlpChatbot, Guid> _nlpChatbotRepository;
        private readonly IRepository<NlpQA, Guid> _nlpQARepository;
        private readonly IRepository<NlpCbModel, Guid> _nlpCbModelRepository;
        private readonly IRepository<SubscribableEdition> _subscribableEditionRepository;
        private readonly IRepository<Tenant> _tenantRepository;
        //private readonly IRepository<NlpTenant> _nlpTenantRepository;

        private readonly ICacheManager _cacheManager;
        private  SemaphoreSlim _semaphoreSlim;


        private static ConcurrentDictionary<int, object> _tenantLock = new ConcurrentDictionary<int, object>();

        private static ConcurrentDictionary<string, SemaphoreSlim> _tenantSemaphoreSlim = new ConcurrentDictionary<string, SemaphoreSlim>();


        public NlpPolicyAppService(IFeatureChecker featureChecker,
            IRepository<NlpChatbot, Guid> nlpChatbotRepository,
            IRepository<NlpQA, Guid> nlpQARepository,
            IRepository<NlpCbModel, Guid> nlpCbModelRepository,
            IRepository<SubscribableEdition> subscribableEditionRepository,
            IRepository<Tenant> tenantRepository,
            //IRepository<NlpTenant> nlpTenantRepository,
            ICacheManager cacheManager)
        {
            _featureChecker = featureChecker;
            _nlpChatbotRepository = nlpChatbotRepository;
            _nlpQARepository = nlpQARepository;

            _nlpCbModelRepository = nlpCbModelRepository;
            _subscribableEditionRepository = subscribableEditionRepository;
            _tenantRepository = tenantRepository;
            //_nlpTenantRepository = nlpTenantRepository;

            _cacheManager = cacheManager;

            _semaphoreSlim = null;
        }

        public async Task CheckMaxChatbotCount(int tenantId, int offset)
        {
            var maxChatbotCount = (_featureChecker.GetValue(tenantId, AppFeatures.MaxChatbotCount)).To<int>();
            if (maxChatbotCount <= 0)
            {
                return;
            }

            var currentChatbotCount = await _nlpChatbotRepository.CountAsync(e => e.IsDeleted == false);
            if (currentChatbotCount + offset > maxChatbotCount)
            {
                throw new UserFriendlyException(L("MaximumChatbotCount_Error_Message"), L("MaximumChatbotCount_Error_Detail", maxChatbotCount));
            }
        }

        private int? _maxQACount;
        private int? _currentQACount;
        public async Task CheckMaxQuestionCount(int tenantId, Guid chatbotId)
        {
            if (_maxQACount.HasValue == false)
            {
                _maxQACount = (_featureChecker.GetValue(tenantId, AppFeatures.MaxQuestionCount)).To<int>();
                if (_maxQACount == 0)
                    return;
            }

            if (_currentQACount.HasValue == false)
            {
                _currentQACount = await _nlpQARepository.GetAll()
                     .Where(e => e.NlpChatbotId == chatbotId && e.QaType != NlpQAConsts.QaType_Unanswerable)
                     .SumAsync(e => e.QuestionCount <= 0 ? 1 : e.QuestionCount);
            }

            _currentQACount++;

            if (_currentQACount > _maxQACount * 1.1)
            {
                throw new UserFriendlyException(L("MaximumQuestionCount_Error_Message"), L("MaximumQuestionCount_Error_Detail", _maxQACount));
            }
        }


        /// <summary>
        /// </summary>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// 若為Free Edition, 前三十天但在限制內，每一個Tenant有自己一個SemaphoreSlim，
        /// 若為Free Edition, 超過三十天或超過限制，所有Tenant共用一個SemaphoreSlim，
        /// 若為付費 Edition, 在合約內，每一個Tenant有自己的SemaphoreSlim(AI Processor)，
        //GetExceedMessageSendQuotaSemaphoreSlim(int tenantId)
        public async Task<SemaphoreSlim> GetMessageSendQuotaSemaphoreSlim(int tenantId)
        {
            if (_semaphoreSlim != null)
                return _semaphoreSlim;

            NlpPolicyItem policyItem = (NlpPolicyItem)_cacheManager.Get_MaxProcessingUnitInfo(tenantId);
            //policyItem = null;

            if (policyItem == null)
            {
                var nlpTenantInfo = await UpdateTenantPriority(tenantId);

                var maxPUCount = (_featureChecker.GetValue(tenantId, AppFeatures.MaxProcessingUnitCount)).To<int>();

                maxPUCount = Math.Max(1, maxPUCount);

                policyItem = new NlpPolicyItem(maxPUCount, maxPUCount, nlpTenantInfo);

                _cacheManager.Set_MaxProcessingUnitInfo(tenantId, policyItem);
            }

            string key;
            int semaphoreCount = 1;

            if (policyItem.NlpTenant.IsFreeEdition)
            {
                if (policyItem.NlpTenant.IsFreeEditionInFirst30Days)
                    key = "F30:" + tenantId.ToString();
                else
                    key = "F";
            }
            else
            {
                if (policyItem.NlpTenant.IsPaidEditionExpired)
                    semaphoreCount = 1;
                else
                    semaphoreCount = policyItem.MaximumCount;

                key = "S:" + tenantId.ToString() + ":" + semaphoreCount.ToString();
            }

            if (_tenantSemaphoreSlim.TryGetValue(key, out _semaphoreSlim))
                return _semaphoreSlim;

            _semaphoreSlim = new SemaphoreSlim(semaphoreCount);
            _tenantSemaphoreSlim[key] = _semaphoreSlim;
            return _semaphoreSlim;
        }

        /// <summary>
        /// 預防使用太多的GetMessage連線 HTTP
        /// </summary>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        public async Task<SemaphoreSlim> Get_GetMessageQuotaSemaphoreSlim(int tenantId)
        {
            NlpPolicyItem policyItem = (NlpPolicyItem)_cacheManager.Get_MaxProcessingUnitInfo(tenantId);
            //policyItem = null;

            if (policyItem == null)
            {
                var nlpTenantInfo = await UpdateTenantPriority(tenantId);

                var maxPUCount = (_featureChecker.GetValue(tenantId, AppFeatures.MaxProcessingUnitCount)).To<int>();

                maxPUCount = Math.Max(1, maxPUCount);

                policyItem = new NlpPolicyItem(maxPUCount, maxPUCount, nlpTenantInfo);

                _cacheManager.Set_MaxProcessingUnitInfo(tenantId, policyItem);
            }

            string key;
            int semaphoreCount = 1;

            if (policyItem.NlpTenant.IsPaidEditionExpired || policyItem.NlpTenant.IsFreeEdition)
                semaphoreCount = 1;
            else
                semaphoreCount = policyItem.MaximumCount * 10;

            key = "G:" + tenantId.ToString() + ":" + semaphoreCount.ToString();

            if (_tenantSemaphoreSlim.TryGetValue(key, out SemaphoreSlim sempahoreSlim))
                return sempahoreSlim;

            sempahoreSlim = new SemaphoreSlim(semaphoreCount);
            _tenantSemaphoreSlim[key] = sempahoreSlim;
            return sempahoreSlim;
        }

        /// <summary>
        /// 更新訓練優先權，優先權低的先安排訓練，
        /// 優先權計算: 最近一個月的總訓練時間/月費(subscriptionAmount)，越低等級越高
        /// </summary>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// 
        [DisableAuditing]
        public async Task<NlpTenantCoreDto> UpdateTenantPriority(int tenantId)
        {
            using (CurrentUnitOfWork.SetTenantId(tenantId))
            {
                var nlpTenant = await _tenantRepository.FirstOrDefaultAsync(tenantId);

                var dt = Clock.Now;
                var dtStart = new DateTime(dt.Year, dt.Month, 1);

                double nlpPriority = nlpTenant.NlpPriority;
                bool isFree = false;
                bool isFreeEditionInFirst30Days = false;
                bool isPaidEditionExpired = false;

                var nlpTenantCoreDto =
                    await (from o in _tenantRepository.GetAll().Where(e => e.Id == tenantId)
                           join o1 in _subscribableEditionRepository.GetAll() on o.EditionId equals o1.Id into j1
                           from s1 in j1.DefaultIfEmpty()
                           select new NlpTenantCoreDto()
                           {
                               IsFreeEdition = s1.IsFree,
                               IsFreeEditionInFirst30Days = (s1.IsFree && s1.CreationTime.AddDays(30) > DateTime.UtcNow),
                               IsPaidEditionExpired = (o.SubscriptionEndDateUtc != null && o.SubscriptionEndDateUtc < DateTime.UtcNow),
                               SubscriptionAmountPerYear = Convert.ToDouble(s1.AnnualPrice.HasValue ? s1.AnnualPrice.Value : (s1.MonthlyPrice.HasValue ? s1.MonthlyPrice.Value * 10 : 0)),
                           }).FirstOrDefaultAsync();

                isFree = nlpTenantCoreDto.IsFreeEdition;
                isFreeEditionInFirst30Days = nlpTenantCoreDto.IsFreeEditionInFirst30Days;
                isPaidEditionExpired = nlpTenantCoreDto.IsPaidEditionExpired;

                var subscriptionInfo = new
                {
                    MaxChatbotCount = (await _featureChecker.GetValueAsync(tenantId, AppFeatures.MaxChatbotCount)).To<int>(),
                    MaxQACount = (await _featureChecker.GetValueAsync(tenantId, AppFeatures.MaxQuestionCount)).To<int>(),
                    MaxUserCount = (await _featureChecker.GetValueAsync(tenantId, AppFeatures.MaxUserCount)).To<int>(),
                };

                var trainings = _nlpCbModelRepository.GetAll().Where(e => e.NlpCbMTrainingStartTime > dtStart && (e.NlpCbMTrainingCompleteTime != null || e.NlpCbMTrainingCancellationTime != null));

                var time = from o in trainings
                           select ((o.NlpCbMTrainingCancellationTime != null ? o.NlpCbMTrainingCancellationTime : o.NlpCbMTrainingCompleteTime) - o.NlpCbMTrainingStartTime);

                var totalTime = (await time.ToListAsync()).Sum(e => e.Value.TotalSeconds) + 1;
                //var totalTime = (await time.AsAsyncEnumerable()).Sum(e => e.Value.TotalSeconds) + 1;

                if (nlpTenantCoreDto.SubscriptionAmountPerYear == 0)
                {
                    nlpTenantCoreDto.SubscriptionAmountPerYear = 1;

                    if (nlpTenantCoreDto.IsFreeEditionInFirst30Days)
                        nlpTenantCoreDto.SubscriptionAmountPerYear = 100;
                }

                nlpPriority = totalTime / nlpTenantCoreDto.SubscriptionAmountPerYear;

                //nlpTenant.NlpPriority = nlpPriority;
                //nlpTenant.SubscriptionAmount = nlpTenantCoreDto.SubscriptionAmountPerYear;

                await _tenantRepository.BatchUpdateAsync(
                    e => new NlpTenant
                    {
                        NlpPriority = nlpPriority,
                        SubscriptionAmount = nlpTenantCoreDto.SubscriptionAmountPerYear
                    },
                    e => e.Id == nlpTenant.Id);

                return new NlpTenantCoreDto()
                {
                    TenantId = nlpTenant.Id,
                    NlpPriority = nlpTenant.NlpPriority,
                    SubscriptionAmountPerYear = nlpTenant.SubscriptionAmount,
                    IsFreeEdition = isFree,
                    IsFreeEditionInFirst30Days = isFreeEditionInFirst30Days,
                    IsPaidEditionExpired = isPaidEditionExpired,
                };
            }
        }
    }
}
