using Abp.Auditing;
using Abp.Authorization;
using Abp.Runtime.Caching;
using Abp.Runtime.Session;
using ApiProtectorDotNet;
using AIaaS.Authorization;
using AIaaS.Tenants.Dashboard.Dto;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AIaaS.Helpers;
using AIaaS.Editions;
using Abp.Application.Features;
using Abp.Domain.Repositories;
using AIaaS.Nlp;
using System;
using AIaaS.Features;
using Abp.Extensions;
using System.Linq;

namespace AIaaS.Tenants.Dashboard
{
    [DisableAuditing]
    [AbpAuthorize(AppPermissions.Pages_Tenant_Dashboard)]
    public class TenantDashboardAppService : AIaaSAppServiceBase, ITenantDashboardAppService
    {

        //private MultiTenancy.Tenant _tenant;
        private readonly IFeatureChecker _featureChecker;
        private readonly IRepository<NlpChatbot, Guid> _nlpChatbotRepository;
        //private readonly IRepository<NlpQA, Guid> _nlpQARepository;
        private readonly IRepository<NlpCbMessage, Guid> _nlpCbMessageRepository;
        private readonly ICacheManager _cacheManager;
        //private readonly IEditionAppService _editionAppService;

        public TenantDashboardAppService(IFeatureChecker featureChecker,
            IRepository<NlpChatbot, Guid> nlpChatbotRepository,
            //IRepository<NlpQA, Guid> nlpQARepository,
            IRepository<NlpCbMessage, Guid> nlpCbMessageRepository,
            ICacheManager cacheManager,
            IEditionAppService editionAppService)
        {
            _featureChecker = featureChecker;
            _nlpChatbotRepository = nlpChatbotRepository;
            //_nlpQARepository = nlpQARepository;
            _nlpCbMessageRepository = nlpCbMessageRepository;
            _cacheManager = cacheManager;
            //_editionAppService = editionAppService;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetMemberActivityOutput GetMemberActivity()
        {
            return new GetMemberActivityOutput
            (
                DashboardRandomDataGenerator.GenerateMemberActivities()
            );
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetDashboardDataOutput GetDashboardData(GetDashboardDataInput input)
        {
            var output = new GetDashboardDataOutput
            {
                TotalProfit = DashboardRandomDataGenerator.GetRandomInt(5000, 9000),
                NewFeedbacks = DashboardRandomDataGenerator.GetRandomInt(1000, 5000),
                NewOrders = DashboardRandomDataGenerator.GetRandomInt(100, 900),
                NewUsers = DashboardRandomDataGenerator.GetRandomInt(50, 500),
                SalesSummary = DashboardRandomDataGenerator.GenerateSalesSummaryData(input.SalesSummaryDatePeriod),
                Expenses = DashboardRandomDataGenerator.GetRandomInt(5000, 10000),
                Growth = DashboardRandomDataGenerator.GetRandomInt(5000, 10000),
                Revenue = DashboardRandomDataGenerator.GetRandomInt(1000, 9000),
                TotalSales = DashboardRandomDataGenerator.GetRandomInt(10000, 90000),
                TransactionPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                NewVisitPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                BouncePercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                DailySales = DashboardRandomDataGenerator.GetRandomArray(30, 10, 50),
                ProfitShares = DashboardRandomDataGenerator.GetRandomPercentageArray(3)
            };

            return output;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetProfitShareOutput GetProfitShare()
        {
            return new GetProfitShareOutput
            {
                ProfitShares = DashboardRandomDataGenerator.GetRandomPercentageArray(3)
            };
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetDailySalesOutput GetDailySales()
        {
            return new GetDailySalesOutput
            {
                DailySales = DashboardRandomDataGenerator.GetRandomArray(30, 10, 50)
            };
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetSalesSummaryOutput GetSalesSummary(GetSalesSummaryInput input)
        {
            var salesSummary = DashboardRandomDataGenerator.GenerateSalesSummaryData(input.SalesSummaryDatePeriod);
            return new GetSalesSummaryOutput(salesSummary)
            {
                Expenses = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
                Growth = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
                Revenue = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
                TotalSales = DashboardRandomDataGenerator.GetRandomInt(0, 3000)
            };
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetRegionalStatsOutput GetRegionalStats()
        {
            return new GetRegionalStatsOutput(
                DashboardRandomDataGenerator.GenerateRegionalStat()
            );
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetGeneralStatsOutput GetGeneralStats()
        {
            return new GetGeneralStatsOutput
            {
                TransactionPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                NewVisitPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
                BouncePercent = DashboardRandomDataGenerator.GetRandomInt(10, 100)
            };
        }


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public GetTopStatsOutput GetTopStats()
        {
            return new GetTopStatsOutput
            {
                TotalProfit = DashboardRandomDataGenerator.GetRandomInt(5000, 9000),
                NewFeedbacks = DashboardRandomDataGenerator.GetRandomInt(1000, 5000),
                NewOrders = DashboardRandomDataGenerator.GetRandomInt(100, 900),
                NewUsers = DashboardRandomDataGenerator.GetRandomInt(50, 500)
            };
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<GetSubscriptionSummaryOutput> GetSubscriptionSummary()
        {
            var subscriptionSummary = (GetSubscriptionSummaryOutput)_cacheManager.Get_Widget(AbpSession.TenantId.Value, "SubscriptionSummary");

            if (subscriptionSummary != null)
                return subscriptionSummary;

            var tenant = await TenantManager.Tenants
                .Include(t => t.Edition).FirstOrDefaultAsync(e => e.Id == AbpSession.TenantId);

            subscriptionSummary = new GetSubscriptionSummaryOutput
            {
                TenantCodeName = tenant.TenancyName,
                EditionName = L(tenant.Edition.DisplayName),
                //EditionName = await _editionAppService.GetNlpEditionLocalShortName(tenant.Edition.Id, ""),
                SubscriptionEndDateUtc = tenant.SubscriptionEndDateUtc,
                IsFree = ((SubscribableEdition)tenant.Edition).IsFree,

                MaxChatbotCount = (await _featureChecker.GetValueAsync(AbpSession.TenantId.Value, AppFeatures.MaxChatbotCount)).To<int>(),
                CurrentChatbotCount = await _nlpChatbotRepository.CountAsync(e => e.IsDeleted == false),

                MaxQACount = (await _featureChecker.GetValueAsync(AbpSession.TenantId.Value, AppFeatures.MaxQuestionCount)).To<int>(),
                //CurrentQACount = await _nlpQARepository.GetAll()
                //                     .Include(e => e.NlpChatbotFk)
                //                     .Where(e => e.NlpChatbotFk.IsDeleted == false && e.QaType != NlpQAConsts.QaType_Unanswerable)
                //                     .SumAsync(e => e.QuestionCount <= 0 ? 1 : e.QuestionCount),

                MaxPUCount = (await _featureChecker.GetValueAsync(AbpSession.TenantId.Value, AppFeatures.MaxProcessingUnitCount)).To<int>(),

                MaxUserCount = (await _featureChecker.GetValueAsync(AbpSession.TenantId.Value, AppFeatures.MaxUserCount)).To<int>(),
                CurrentUserCount = await UserManager.Users.CountAsync(e => e.IsDeleted == false),
            };

            _cacheManager.Set_Widget(AbpSession.TenantId.Value, "SubscriptionSummary", subscriptionSummary);

            return subscriptionSummary;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<GetQAStatisticsDataOutput> GetQAStatistics(GetQAStatisticsDataInput input)
        {
            if (input.StartDate.AddMonths(3) < input.EndDate)
                input.StartDate = input.EndDate.AddMonths(-3);

            var minDate = input.StartDate.Date;
            var maxDate = input.EndDate.Date;

            var widgetData = (GetQAStatisticsDataOutput)_cacheManager.Get_Widget(AbpSession.TenantId.Value, "QAStatistics" + (minDate.Ticks + maxDate.Ticks).ToString());

            if (widgetData != null)
                return widgetData;


            var filterMessage = _nlpCbMessageRepository.GetAll()
                .Where(e => e.NlpCreationTime >= input.StartDate && e.NlpCreationTime <= input.EndDate && e.TenantId == AbpSession.TenantId.Value && e.NlpSenderRole == "chatbot")
                .Select(e => e.NlpCreationTime.Date)
                .GroupBy(e => e)
                .Select(g => new StastisticBase(g.Key, Convert.ToDecimal(g.Count())));

            var dictionary = await filterMessage.ToDictionaryAsync(e => e.Date);

            for (var dt = minDate; dt <= maxDate; dt = dt.AddDays(1))
                if (dictionary.ContainsKey(dt) == false)
                    dictionary[dt] = new StastisticBase(dt, 0);

            var output = new GetQAStatisticsDataOutput(dictionary.Values.OrderBy(e => e.Date).ToList());

            _cacheManager.Set_Widget(AbpSession.TenantId.Value, "QAStatistics" + (minDate.Ticks + maxDate.Ticks).ToString(), output);

            return output;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<GetVisitorStatisticsDataOutput> GetVisitorStatistics(GetVisitorStatisticsDataInput input)
        {
            if (input.StartDate.AddMonths(3) < input.EndDate)
                input.StartDate = input.EndDate.AddMonths(-3);

            var minDate = input.StartDate.Date;
            var maxDate = input.EndDate.Date;

            var widgetData = (GetVisitorStatisticsDataOutput)_cacheManager.Get_Widget(AbpSession.TenantId.Value, "VisitorStatistics" + (minDate.Ticks + maxDate.Ticks).ToString());

            if (widgetData != null)
                return widgetData;

            var filterMessage = _nlpCbMessageRepository.GetAll()
                .Where(e => e.NlpCreationTime >= input.StartDate && e.NlpCreationTime <= input.EndDate && e.TenantId == AbpSession.TenantId.Value && e.NlpSenderRole == "client" && e.ClientId != null)
                .Select(e => new { Date = e.NlpCreationTime.Date, ClientId = e.ClientId.Value }).Distinct()
                .GroupBy(a => a.Date)
                .Select(g => new StastisticBase(g.Key, Convert.ToDecimal(g.Count())));

            var dictionary = await filterMessage.ToDictionaryAsync(e => e.Date);

            for (var dt = minDate; dt <= maxDate; dt = dt.AddDays(1))
                if (dictionary.ContainsKey(dt) == false)
                    dictionary[dt] = new StastisticBase(dt, 0);

            var output = new GetVisitorStatisticsDataOutput(dictionary.Values.OrderBy(e => e.Date).ToList());

            _cacheManager.Set_Widget(AbpSession.TenantId.Value, "VisitorStatistics" + (minDate.Ticks + maxDate.Ticks).ToString(), output);

            return output;
        }
    }
}