using Abp.Application.Services;
using AIaaS.Tenants.Dashboard.Dto;
using System.Threading.Tasks;

namespace AIaaS.Tenants.Dashboard
{
    public interface ITenantDashboardAppService : IApplicationService
    {
        GetMemberActivityOutput GetMemberActivity();

        GetDashboardDataOutput GetDashboardData(GetDashboardDataInput input);

        GetDailySalesOutput GetDailySales();

        GetProfitShareOutput GetProfitShare();

        GetSalesSummaryOutput GetSalesSummary(GetSalesSummaryInput input);

        GetTopStatsOutput GetTopStats();

        GetRegionalStatsOutput GetRegionalStats();

        GetGeneralStatsOutput GetGeneralStats();

        Task<GetSubscriptionSummaryOutput> GetSubscriptionSummary();

        Task<GetQAStatisticsDataOutput> GetQAStatistics(GetQAStatisticsDataInput input);
        //Task<GetSubscriptionStatsOutput> GetSubscriptionStats();

        Task<GetVisitorStatisticsDataOutput> GetVisitorStatistics(GetVisitorStatisticsDataInput input);

    }
}
