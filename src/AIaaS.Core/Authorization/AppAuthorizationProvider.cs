using Abp.Authorization;
using Abp.Configuration.Startup;
using Abp.Localization;
using Abp.MultiTenancy;

namespace AIaaS.Authorization
{
    /// <summary>
    /// Application's authorization provider.
    /// Defines permissions for the application.
    /// See <see cref="AppPermissions"/> for all permission names.
    /// </summary>
    public class AppAuthorizationProvider : AuthorizationProvider
    {
        private readonly bool _isMultiTenancyEnabled;

        public AppAuthorizationProvider(bool isMultiTenancyEnabled)
        {
            _isMultiTenancyEnabled = isMultiTenancyEnabled;
        }

        public AppAuthorizationProvider(IMultiTenancyConfig multiTenancyConfig)
        {
            _isMultiTenancyEnabled = multiTenancyConfig.IsEnabled;
        }

        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            //COMMON PERMISSIONS (FOR BOTH OF TENANTS AND HOST)

            var pages = context.GetPermissionOrNull(AppPermissions.Pages) ?? context.CreatePermission(AppPermissions.Pages, L("Pages"));

            pages.CreateChildPermission(AppPermissions.Pages_Tenant_Dashboard, L("Dashboard"), multiTenancySides: MultiTenancySides.Tenant);

            pages.CreateChildPermission(AppPermissions.Pages_DemoUiComponents, L("DemoUiComponents"), multiTenancySides: MultiTenancySides.Host);

            var nlpChatbot = pages.CreateChildPermission(AppPermissions.Pages_NlpChatbot, L("NlpChatbotService"));

            var nlpChatbots = nlpChatbot.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpChatbots, L("NlpChatbot"), multiTenancySides: MultiTenancySides.Tenant);
            nlpChatbots.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpChatbots_Create, L("CreateNewNlpChatbot"), multiTenancySides: MultiTenancySides.Tenant);
            nlpChatbots.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpChatbots_Edit, L("EditNlpChatbot"), multiTenancySides: MultiTenancySides.Tenant);
            nlpChatbots.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpChatbots_Delete, L("DeleteNlpChatbot"), multiTenancySides: MultiTenancySides.Tenant);
            nlpChatbots.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpChatbots_Train, L("TrainNlpChatbot"), multiTenancySides: MultiTenancySides.Tenant);
            nlpChatbots.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpChatbots_Import, L("ImportNlpChatbot"), multiTenancySides: MultiTenancySides.Tenant);
            nlpChatbots.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpChatbots_Export, L("ExportNlpChatbot"), multiTenancySides: MultiTenancySides.Tenant);

            var nlpQAs = nlpChatbot.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpQAs, L("NlpQAs"), multiTenancySides: MultiTenancySides.Tenant);
            nlpQAs.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpQAs_Create, L("CreateNewNlpQA"), multiTenancySides: MultiTenancySides.Tenant);
            nlpQAs.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpQAs_Edit, L("EditNlpQA"), multiTenancySides: MultiTenancySides.Tenant);
            nlpQAs.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpQAs_Delete, L("DeleteNlpQA"), multiTenancySides: MultiTenancySides.Tenant);
            nlpQAs.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpQAs_Import, L("ImportNlpQA"), multiTenancySides: MultiTenancySides.Tenant);
            nlpQAs.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpQAs_Export, L("ExportNlpQA"), multiTenancySides: MultiTenancySides.Tenant);

            var nlpWorkflows = nlpChatbot.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpWorkflows, L("NlpWorkflows"), multiTenancySides: MultiTenancySides.Tenant);
            nlpWorkflows.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Create, L("CreateNewNlpWorkflow"), multiTenancySides: MultiTenancySides.Tenant);
            nlpWorkflows.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Edit, L("EditNlpWorkflow"), multiTenancySides: MultiTenancySides.Tenant);
            nlpWorkflows.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Delete, L("DeleteNlpWorkflow"), multiTenancySides: MultiTenancySides.Tenant);

            var nlpCbModels = nlpChatbot.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpCbModels, L("NlpCbModels"), multiTenancySides: MultiTenancySides.Tenant);

            var nlpCbMessages = nlpChatbot.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpCbMessages, L("NlpCbMessageHistory"), multiTenancySides: MultiTenancySides.Tenant);

            var nlpCbQAAccuracies = nlpChatbot.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpCbQAAccuracies, L("NlpCbQAAccuracies"), multiTenancySides: MultiTenancySides.Tenant);

            var nlpCbAgentOperations = nlpChatbot.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpCbAgentOperations, L("NlpCbAgentOperations"), multiTenancySides: MultiTenancySides.Tenant);
            nlpCbAgentOperations.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpCbAgentOperations_SendMessage, L("NlpCbAgentSendMessage"), multiTenancySides: MultiTenancySides.Tenant);

            var nlpTokens = nlpChatbot.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpTokens, L("NlpTokens"), multiTenancySides: MultiTenancySides.Host);
            nlpTokens.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpTokens_Create, L("CreateNewNlpToken"), multiTenancySides: MultiTenancySides.Host);
            nlpTokens.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpTokens_Edit, L("EditNlpToken"), multiTenancySides: MultiTenancySides.Host);
            nlpTokens.CreateChildPermission(AppPermissions.Pages_NlpChatbot_NlpTokens_Delete, L("DeleteNlpToken"), multiTenancySides: MultiTenancySides.Host);

            var administration = pages.CreateChildPermission(AppPermissions.Pages_Administration, L("Administration"));

            var organizationUnits = administration.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits, L("OrganizationUnits"));
            organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageOrganizationTree, L("ManagingOrganizationTree"));
            organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageMembers, L("ManagingMembers"));
            organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageRoles, L("ManagingRoles"));

            var roles = administration.CreateChildPermission(AppPermissions.Pages_Administration_Roles, L("Roles"));
            roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Create, L("CreatingNewRole"));
            roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Edit, L("EditingRole"));
            roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Delete, L("DeletingRole"));

            var users = administration.CreateChildPermission(AppPermissions.Pages_Administration_Users, L("Users"));
            users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Create, L("CreatingNewUser"));
            users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Edit, L("EditingUser"));
            users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Delete, L("DeletingUser"));
            users.CreateChildPermission(AppPermissions.Pages_Administration_Users_ChangePermissions, L("ChangingPermissions"));
            users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Impersonation, L("LoginForUsers"));
            users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Unlock, L("Unlock"));
            users.CreateChildPermission(AppPermissions.Pages_Administration_Users_ChangeProfilePicture, L("UpdateUsersProfilePicture"));

            var languages = administration.CreateChildPermission(AppPermissions.Pages_Administration_Languages, L("Languages"));
            languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Create, L("CreatingNewLanguage"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
            languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Edit, L("EditingLanguage"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
            languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Delete, L("DeletingLanguages"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
            languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_ChangeTexts, L("ChangingTexts"));
            languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_ChangeDefaultLanguage, L("ChangeDefaultLanguage"));

            administration.CreateChildPermission(AppPermissions.Pages_Administration_AuditLogs, L("AuditLogs"));

            administration.CreateChildPermission(AppPermissions.Pages_Administration_Tenant_SubscriptionManagement, L("Subscription"), multiTenancySides: MultiTenancySides.Tenant);

            administration.CreateChildPermission(AppPermissions.Pages_Administration_UiCustomization, L("VisualSettings"));

            var webhooks = administration.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription, L("Webhooks"));
            webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Create, L("CreatingWebhooks"));
            webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Edit, L("EditingWebhooks"));
            webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_ChangeActivity, L("ChangingWebhookActivity"));
            webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Detail, L("DetailingSubscription"));
            webhooks.CreateChildPermission(AppPermissions.Pages_Administration_Webhook_ListSendAttempts, L("ListingSendAttempts"));
            webhooks.CreateChildPermission(AppPermissions.Pages_Administration_Webhook_ResendWebhook, L("ResendingWebhook"));

            var dynamicProperties = administration.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties, L("DynamicProperties"));
            dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Create, L("CreatingDynamicProperties"));
            dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Edit, L("EditingDynamicProperties"));
            dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Delete, L("DeletingDynamicProperties"));

            var dynamicPropertyValues = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue, L("DynamicPropertyValue"));
            dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Create, L("CreatingDynamicPropertyValue"));
            dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Edit, L("EditingDynamicPropertyValue"));
            dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Delete, L("DeletingDynamicPropertyValue"));

            var dynamicEntityProperties = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties, L("DynamicEntityProperties"));
            dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Create, L("CreatingDynamicEntityProperties"));
            dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Edit, L("EditingDynamicEntityProperties"));
            dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Delete, L("DeletingDynamicEntityProperties"));

            var dynamicEntityPropertyValues = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue, L("EntityDynamicPropertyValue"));
            dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Create, L("CreatingDynamicEntityPropertyValue"));
            dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Edit, L("EditingDynamicEntityPropertyValue"));
            dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Delete, L("DeletingDynamicEntityPropertyValue"));

            var massNotification = administration.CreateChildPermission(AppPermissions.Pages_Administration_MassNotification, L("MassNotifications"));
            massNotification.CreateChildPermission(AppPermissions.Pages_Administration_MassNotification_Create, L("MassNotificationCreate"));
            
            //TENANT-SPECIFIC PERMISSIONS

            administration.CreateChildPermission(AppPermissions.Pages_Administration_Tenant_Settings, L("Settings"), multiTenancySides: MultiTenancySides.Tenant);

            //HOST-SPECIFIC PERMISSIONS

            var editions = pages.CreateChildPermission(AppPermissions.Pages_Editions, L("Editions"), multiTenancySides: MultiTenancySides.Host);
            editions.CreateChildPermission(AppPermissions.Pages_Editions_Create, L("CreatingNewEdition"), multiTenancySides: MultiTenancySides.Host);
            editions.CreateChildPermission(AppPermissions.Pages_Editions_Edit, L("EditingEdition"), multiTenancySides: MultiTenancySides.Host);
            editions.CreateChildPermission(AppPermissions.Pages_Editions_Delete, L("DeletingEdition"), multiTenancySides: MultiTenancySides.Host);
            editions.CreateChildPermission(AppPermissions.Pages_Editions_MoveTenantsToAnotherEdition, L("MoveTenantsToAnotherEdition"), multiTenancySides: MultiTenancySides.Host);

            var tenants = pages.CreateChildPermission(AppPermissions.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
            tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Create, L("CreatingNewTenant"), multiTenancySides: MultiTenancySides.Host);
            tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Edit, L("EditingTenant"), multiTenancySides: MultiTenancySides.Host);
            tenants.CreateChildPermission(AppPermissions.Pages_Tenants_ChangeFeatures, L("ChangingFeatures"), multiTenancySides: MultiTenancySides.Host);
            tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Delete, L("DeletingTenant"), multiTenancySides: MultiTenancySides.Host);
            tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Impersonation, L("LoginForTenants"), multiTenancySides: MultiTenancySides.Host);

            administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Settings, L("Settings"), multiTenancySides: MultiTenancySides.Host);
            
            var maintenance = administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Maintenance, L("Maintenance"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
            maintenance.CreateChildPermission(AppPermissions.Pages_Administration_NewVersion_Create, L("SendNewVersionNotification"));
            
            administration.CreateChildPermission(AppPermissions.Pages_Administration_HangfireDashboard, L("HangfireDashboard"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
            administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Dashboard, L("Dashboard"), multiTenancySides: MultiTenancySides.Host);
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, AIaaSConsts.LocalizationSourceName);
        }
    }
}
