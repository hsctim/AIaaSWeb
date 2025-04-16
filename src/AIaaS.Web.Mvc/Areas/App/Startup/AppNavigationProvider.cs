using Abp.Application.Navigation;
using Abp.Authorization;
using Abp.Localization;
using AIaaS.Authorization;

namespace AIaaS.Web.Areas.App.Startup
{
    public class AppNavigationProvider : NavigationProvider
    {
        public const string MenuName = "App";

        public override void SetNavigation(INavigationProviderContext context)
        {
            var menu = context.Manager.Menus[MenuName] = new MenuDefinition(
                MenuName, new FixedLocalizableString("Main Menu"));

            menu
                .AddItem(new MenuItemDefinition(
                    AppPageNames.Host.Dashboard, L("Dashboard"),
                    url: "App/HostDashboard", icon: "flaticon-line-graph",
                    permissionDependency: new SimplePermissionDependency(
                        AppPermissions.Pages_Administration_Host_Dashboard)))
                .AddItem(new MenuItemDefinition(
                    AppPageNames.Host.Tenants, L("Tenants"), url: "App/Tenants",
                    icon: "flaticon-list-3",
                    permissionDependency: new SimplePermissionDependency(
                        AppPermissions.Pages_Tenants)))
                .AddItem(new MenuItemDefinition(
                    AppPageNames.Host.Editions, L("Editions"),
                    url: "App/Editions", icon: "flaticon-app",
                    permissionDependency: new SimplePermissionDependency(
                        AppPermissions.Pages_Editions)))
                .AddItem(new MenuItemDefinition(
                    AppPageNames.Tenant.Dashboard, L("Dashboard"),
                    url: "App/TenantDashboard", icon: "flaticon-line-graph",
                    permissionDependency: new SimplePermissionDependency(
                        AppPermissions.Pages_Tenant_Dashboard)))
                .AddItem(
                    new MenuItemDefinition(
                        AppPageNames.Tenant.NlpChatbotService,
                        L("NlpChatbotService"), url: "App/NlpChatbots",
                        icon: "far fa-user-circle")
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Host.NlpTokens, L("NlpTokens"),
                            url: "App/NlpTokens", icon: "flaticon-more",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_NlpChatbot_NlpTokens)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.NlpChatbots, L("NlpChatbots"),
                            url: "App/NlpChatbots", icon: "far fa-user-circle",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_NlpChatbot_NlpChatbots)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.NlpQAs, L("NlpQAs"),
                            url: "App/NlpQAs", icon: "fas fa-list",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_NlpChatbot_NlpQAs)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.NlpWorkflows, L("NlpWorkflows"),
                            url: "App/NlpWorkflows", icon: "flaticon-cogwheel",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_NlpChatbot_NlpWorkflows))
                                                .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.NlpWorkflows, L("NlpWorkflows"),
                            url: "App/NlpWorkflows", icon: "flaticon-more-v4",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_NlpChatbot_NlpWorkflows)))

                            .AddItem(new MenuItemDefinition(
                                AppPageNames.Tenant.NlpWorkflows, L("NlpWorkflowStates"),
                                url: "App/NlpWorkflows/NlpWorkflowstates", icon: "flaticon-grid-menu-v2",
                                permissionDependency: new SimplePermissionDependency(
                                    AppPermissions.Pages_NlpChatbot_NlpWorkflows)))

                        )
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.NlpCbModels, L("NlpCbModels"),
                            url: "App/NlpCbModels", icon: "fas fa-history",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_NlpChatbot_NlpCbModels)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.NlpCbMessages,
                            L("NlpCbMessageHistory"), url: "App/NlpCbMessages",
                            icon: "flaticon-chat-1",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_NlpChatbot_NlpCbMessages)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.NlpCbQAAccuracies,
                            L("NlpCbQAAccuracies"),
                            url: "App/NlpCbQAAccuracies",
                            icon: "flaticon-analytics",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_NlpChatbot_NlpCbQAAccuracies)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.NlpCbAgentOperations,
                            L("NlpCbAgentOperations"),
                            url: "App/NlpCbAgentOperations",
                            icon: "flaticon2-chat-1",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_NlpChatbot_NlpCbAgentOperations)))

                        )
                .AddItem(
                    new MenuItemDefinition(AppPageNames.Common.Administration,
                                           L("Administration"),
                                           icon: "flaticon-interface-8")
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.OrganizationUnits,
                            L("OrganizationUnits"),
                            url: "App/OrganizationUnits", icon: "flaticon-map",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_Administration_OrganizationUnits)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.Roles, L("Roles"),
                            url: "App/Roles", icon: "flaticon-suitcase",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_Administration_Roles)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.Users, L("Users"),
                            url: "App/Users", icon: "flaticon-users",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_Administration_Users)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.Languages, L("Languages"),
                            url: "App/Languages", icon: "flaticon-tabs",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_Administration_Languages)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.AuditLogs, L("AuditLogs"),
                            url: "App/AuditLogs", icon: "flaticon-folder-1",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_Administration_AuditLogs)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Host.Maintenance, L("Maintenance"),
                            url: "App/Maintenance", icon: "flaticon-lock",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_Administration_Host_Maintenance)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.SubscriptionManagement,
                            L("Subscription"),
                            url: "App/SubscriptionManagement",
                            icon: "flaticon-refresh",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_Administration_Tenant_SubscriptionManagement)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.UiCustomization,
                            L("VisualSettings"), url: "App/UiCustomization",
                            icon: "flaticon-medical",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_Administration_UiCustomization)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.WebhookSubscriptions,
                            L("WebhookSubscriptions"),
                            url: "App/WebhookSubscription",
                            icon: "flaticon2-world",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_Administration_WebhookSubscription)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.DynamicProperties,
                            L("DynamicProperties"), url: "App/DynamicProperty",
                            icon: "flaticon-interface-8",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_Administration_DynamicProperties)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Host.Settings, L("Settings"),
                            url: "App/HostSettings", icon: "flaticon-settings",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_Administration_Host_Settings)))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Tenant.Settings, L("Settings"),
                            url: "App/Settings", icon: "flaticon-settings",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions
                                    .Pages_Administration_Tenant_Settings))))
                .AddItem(
                    new MenuItemDefinition(AppPageNames.Document.Main,
                                           L("Document"),
                                           icon: "flaticon2-document")
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Document.GettingStarted,
                            L("Document:Getting_Started"),
                            url: "App/Document/Getting_Started",
                            icon: "flaticon2-paper"))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Document.SupportPOS,
                            L("Document:Support_POS"),
                            url: "App/Document/Support_POS",
                            icon: "flaticon2-paper"))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Document.ImprovingPrediction,
                            L("Document:Improving_Prediction"),
                            url: "App/Document/Improving_Prediction",
                            icon: "flaticon2-paper"))
                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Document.WebAPI, L("Document:WebAPI"),
                            url: "App/Document/WebAPI",
                            icon: "flaticon2-paper"))


                        )

                        //.AddItem(
                        //    new MenuItemDefinition(
                        //        AppPageNames.Common.Notifications,
                        //        L("Notifications"), icon: "flaticon-alarm")
                        //        .AddItem(
                        //            new MenuItemDefinition(
                        //                AppPageNames.Common.Notifications_Inbox,
                        //                L("Inbox"), url: "App/Notifications",
                        //                icon: "flaticon-mail-1")
                        //            )

                        //            .AddItem(new MenuItemDefinition(
                        //                AppPageNames.Common
                        //                    .Notifications_MassNotifications,
                        //                L("MassNotifications"),
                        //                url: "App/Notifications/MassNotifications",
                        //                icon: "flaticon-paper-plane",
                        //                permissionDependency: new SimplePermissionDependency(
                        //                    AppPermissions
                        //                        .Pages_Administration_MassNotification)))
                        //            )

                        .AddItem(
                            new MenuItemDefinition(AppPageNames.Info.Main,
                                                   L("Info:Info"),
                                                   icon: "flaticon-information")
                                .AddItem(
                                    new MenuItemDefinition(
                                        AppPageNames.Common.Notifications,
                                        L("Notifications"), 
                                        url: "App/Notifications",
                                        icon: "flaticon-alarm")
                                    )

                                .AddItem(new MenuItemDefinition(
                                    AppPageNames.Info.ContactUs,
                                    L("Info:ContactUs"), 
                                    url: "/App/ContactUs",
                                    icon: "flaticon-email")
                                )
                                .AddItem(new MenuItemDefinition(
                                    AppPageNames.Info.Privacy,
                                    L("Info:Privacy"),
                                    url: "App/Document/Privacy",
                                    icon: "flaticon-information")
                                )
                                .AddItem(new MenuItemDefinition(
                                    AppPageNames.Info.Terms, L("Info:Terms"),
                                    url: "App/Document/Terms",
                                    icon: "flaticon-information")
                                )
                            )

                        .AddItem(new MenuItemDefinition(
                            AppPageNames.Common.DemoUiComponents,
                            L("DemoUiComponents"), url: "App/DemoUiComponents",
                            icon: "flaticon-shapes",
                            permissionDependency: new SimplePermissionDependency(
                                AppPermissions.Pages_DemoUiComponents)));
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name,
                                         AIaaSConsts.LocalizationSourceName);
        }
    }
}
