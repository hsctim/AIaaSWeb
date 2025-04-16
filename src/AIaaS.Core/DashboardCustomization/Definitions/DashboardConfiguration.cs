using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Authorization;
using Abp.MultiTenancy;
using AIaaS.Authorization;

namespace AIaaS.DashboardCustomization.Definitions
{
    public class DashboardConfiguration
    {
        public List<DashboardDefinition> DashboardDefinitions { get; } = new List<DashboardDefinition>();

        public List<WidgetDefinition> WidgetDefinitions { get; } = new List<WidgetDefinition>();

        public List<WidgetFilterDefinition> WidgetFilterDefinitions { get; } = new List<WidgetFilterDefinition>();

        public DashboardConfiguration()
        {
            #region FilterDefinitions

            // These are global filter which all widgets can use
            var dateRangeFilter = new WidgetFilterDefinition(
                AIaaSDashboardCustomizationConsts.Filters.FilterDateRangePicker,
                "FilterDateRangePicker"
            );

            WidgetFilterDefinitions.Add(dateRangeFilter);

            // Add your filters here

            #endregion

            #region WidgetDefinitions

            // Define Widgets

            #region TenantWidgets

            var simplePermissionDependencyForTenantDashboard = new SimplePermissionDependency(AppPermissions.Pages_Tenant_Dashboard);

            var dailySales = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Tenant.DailySales,
                "WidgetDailySales",
                side: MultiTenancySides.Tenant,
                usedWidgetFilters: new List<string> { dateRangeFilter.Id },
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );

            var generalStats = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Tenant.GeneralStats,
                "WidgetGeneralStats",
                side: MultiTenancySides.Tenant,
                permissionDependency: new SimplePermissionDependency(
                    requiresAll: true,
                    AppPermissions.Pages_Tenant_Dashboard,
                    AppPermissions.Pages_Administration_AuditLogs
                )
            );

            var profitShare = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Tenant.ProfitShare,
                "WidgetProfitShare",
                side: MultiTenancySides.Tenant,
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );

            var memberActivity = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Tenant.MemberActivity,
                "WidgetMemberActivity",
                side: MultiTenancySides.Tenant,
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );

            var regionalStats = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Tenant.RegionalStats,
                "WidgetRegionalStats",
                side: MultiTenancySides.Tenant,
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );

            var salesSummary = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Tenant.SalesSummary,
                "WidgetSalesSummary",
                usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
                side: MultiTenancySides.Tenant,
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );

            var subscriptionSummary = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Tenant.SubscriptionSummary,
                "WidgetSubscriptionSummary",
                usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
                side: MultiTenancySides.Tenant,
                permissionDependency: simplePermissionDependencyForTenantDashboard);

            var qaStatistics = new WidgetDefinition(
                 AIaaSDashboardCustomizationConsts.Widgets.Tenant.QAStatistics,
                 "WidgetQAStatistics",
                 usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
                 side: MultiTenancySides.Tenant,
                 permissionDependency: simplePermissionDependencyForTenantDashboard);

            var visitorStatistics = new WidgetDefinition(
                 AIaaSDashboardCustomizationConsts.Widgets.Tenant.VisitorStatistics,
                 "VisitorStatistics",
                 usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
                 side: MultiTenancySides.Tenant,
                 permissionDependency: simplePermissionDependencyForTenantDashboard);

            //var subscriptionStats = new WidgetDefinition(
            //    AIaaSDashboardCustomizationConsts.Widgets.Tenant.SubscriptionStats,
            //    "WidgetSubscriptionStats",
            //    usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
            //    side: MultiTenancySides.Tenant,
            //    permissions: simplePermissionDependencyForTenantDashboard);


            var topStats = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Tenant.TopStats,
                "WidgetTopStats",
                side: MultiTenancySides.Tenant,
                permissionDependency: simplePermissionDependencyForTenantDashboard
            );



#if DEBUG
            bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            if (isDevelopment)
            {
                WidgetDefinitions.Add(generalStats);
                WidgetDefinitions.Add(dailySales);
                WidgetDefinitions.Add(profitShare);
                WidgetDefinitions.Add(memberActivity);
                WidgetDefinitions.Add(regionalStats);
                WidgetDefinitions.Add(topStats);
                WidgetDefinitions.Add(salesSummary);
            }
#endif

            WidgetDefinitions.Add(subscriptionSummary);
            WidgetDefinitions.Add(qaStatistics);
            WidgetDefinitions.Add(visitorStatistics);

            //WidgetDefinitions.Add(subscriptionStats);

            // Add your tenant side widgets here

            #endregion

            #region HostWidgets

            var simplePermissionDependencyForHostDashboard = new SimplePermissionDependency(AppPermissions.Pages_Administration_Host_Dashboard);

            var incomeStatistics = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Host.IncomeStatistics,
                "WidgetIncomeStatistics",
                side: MultiTenancySides.Host,
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            var hostTopStats = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Host.TopStats,
                "WidgetTopStats",
                side: MultiTenancySides.Host,
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            var editionStatistics = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Host.EditionStatistics,
                "WidgetEditionStatistics",
                side: MultiTenancySides.Host,
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            var subscriptionExpiringTenants = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Host.SubscriptionExpiringTenants,
                "WidgetSubscriptionExpiringTenants",
                side: MultiTenancySides.Host,
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            var recentTenants = new WidgetDefinition(
                AIaaSDashboardCustomizationConsts.Widgets.Host.RecentTenants,
                "WidgetRecentTenants",
                side: MultiTenancySides.Host,
                usedWidgetFilters: new List<string>() { dateRangeFilter.Id },
                permissionDependency: simplePermissionDependencyForHostDashboard
            );

            WidgetDefinitions.Add(incomeStatistics);
            WidgetDefinitions.Add(hostTopStats);
            WidgetDefinitions.Add(editionStatistics);
            WidgetDefinitions.Add(subscriptionExpiringTenants);
            WidgetDefinitions.Add(recentTenants);

            // Add your host side widgets here

            #endregion

            #endregion

            #region DashboardDefinitions

            // Create dashboard

            var defaultTenantDashboard = new DashboardDefinition(
                AIaaSDashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard,
               WidgetDefinitions.Where(e => e.Side == MultiTenancySides.Tenant).Select(e => e.Id).ToList());

            DashboardDefinitions.Add(defaultTenantDashboard);

            var defaultHostDashboard = new DashboardDefinition(
                AIaaSDashboardCustomizationConsts.DashboardNames.DefaultHostDashboard,
                WidgetDefinitions.Where(e => e.Side == MultiTenancySides.Host).Select(e => e.Id).ToList());

            DashboardDefinitions.Add(defaultHostDashboard);

            // Add your dashboard definition here

            #endregion

        }

    }
}
