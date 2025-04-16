namespace AIaaS.Web.Areas.App.Startup
{
    public class AppPageNames
    {
        public static class Common
        {
            public const string Administration = "Administration";
            public const string Roles = "Administration.Roles";
            public const string Users = "Administration.Users";
            public const string AuditLogs = "Administration.AuditLogs";
            public const string OrganizationUnits = "Administration.OrganizationUnits";
            public const string Languages = "Administration.Languages";
            public const string DemoUiComponents = "Administration.DemoUiComponents";
            public const string UiCustomization = "Administration.UiCustomization";
            public const string WebhookSubscriptions = "Administration.WebhookSubscriptions";
            public const string DynamicProperties = "Administration.DynamicProperties";
            public const string DynamicEntityProperties = "Administration.DynamicEntityProperties";
            public const string ContactUs = "Administration.ContactUs";
            public const string Notifications = "Administration.Notifications";
            public const string Notifications_Inbox = "Administration.Notifications.Inbox";
            public const string Notifications_MassNotifications = "Administration.Notifications.MassNotifications";
           
        }

        public static class Document
        {
            public const string Main = "Document";
            public const string GettingStarted = "Document.GettingStarted";
            public const string SupportPOS = "Document.SupportPOS";
            public const string ImprovingPrediction = "Document.ImprovingPrediction";
            public const string WebAPI = "Document.WebAPI";
        }

        public static class Info
        {
            public const string Main = "Info";
            public const string ContactUs = "Info.ContactUs";
            public const string Privacy = "Info.Privacy";
            public const string Terms = "Info.Terms";
        }

        public static class Host
        {
            public const string NlpTokens = "Nlp.NlpTokens";
            public const string Tenants = "Tenants";
            public const string Editions = "Editions";
            public const string Maintenance = "Administration.Maintenance";
            public const string Settings = "Administration.Settings.Host";
            public const string Dashboard = "Dashboard";
        }

        public static class Tenant
        {
            public const string NlpChatbotService = "Administration.Nlp.NlpChatbot.Service";

            public const string NlpWorkflowStates = "Administration.Nlp.NlpWorkflowStates";
            public const string NlpWorkflows = "Administration.Nlp.NlpWorkflows";
            public const string NlpCbAgentOperations = "Administration.Nlp.NlpCbAgentOperations";
            //public const string TenantNlpQALibraries = "Administration.Nlp.NlpQALibraries";
            public const string NlpCbMessages = "Administration.Nlp.NlpCbMessages";
            public const string NlpCbQAAccuracies = "Administration.Nlp.NlpCbQAAccuracies";
            public const string NlpCbModels = "Administration.Nlp.NlpCbModels";
            public const string NlpQAs = "Administration.Nlp.NlpQAs";
            public const string NlpChatbots = "Administration.Nlp.NlpChatbots";
            public const string Dashboard = "Dashboard.Tenant";
            public const string Settings = "Administration.Settings.Tenant";
            public const string SubscriptionManagement = "Administration.SubscriptionManagement.Tenant";
        }
    }
}
