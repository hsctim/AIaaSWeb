using AIaaS.Nlp.Dtos;
using AIaaS.Nlp;
using Abp.Application.Editions;
using Abp.Application.Features;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.DynamicEntityProperties;
using Abp.EntityHistory;
using Abp.Localization;
using Abp.Notifications;
using Abp.Organizations;
using Abp.UI.Inputs;
using Abp.Webhooks;
using AutoMapper;
using IdentityServer4.Extensions;
using AIaaS.Auditing.Dto;
using AIaaS.Authorization.Accounts.Dto;
using AIaaS.Authorization.Delegation;
using AIaaS.Authorization.Permissions.Dto;
using AIaaS.Authorization.Roles;
using AIaaS.Authorization.Roles.Dto;
using AIaaS.Authorization.Users;
using AIaaS.Authorization.Users.Delegation.Dto;
using AIaaS.Authorization.Users.Dto;
using AIaaS.Authorization.Users.Importing.Dto;
using AIaaS.Authorization.Users.Profile.Dto;
using AIaaS.Chat;
using AIaaS.Chat.Dto;
using AIaaS.DynamicEntityProperties.Dto;
using AIaaS.Editions;
using AIaaS.Editions.Dto;
using AIaaS.Friendships;
using AIaaS.Friendships.Cache;
using AIaaS.Friendships.Dto;
using AIaaS.Localization.Dto;
using AIaaS.MultiTenancy;
using AIaaS.MultiTenancy.Dto;
using AIaaS.MultiTenancy.HostDashboard.Dto;
using AIaaS.MultiTenancy.Payments;
using AIaaS.MultiTenancy.Payments.Dto;
using AIaaS.Notifications.Dto;
using AIaaS.Organizations.Dto;
using AIaaS.Sessions.Dto;
using AIaaS.WebHooks.Dto;
using AIaaS.Nlp.ImExport;

namespace AIaaS
{
    internal static class CustomDtoMapper
    {
        public static void CreateMappings(IMapperConfigurationExpression configuration)
        {
            configuration.CreateMap<CreateOrEditNlpWorkflowStateDto, NlpWorkflowState>().ReverseMap();
            configuration.CreateMap<NlpWorkflowState, NlpWorkflowStateDto>().ReverseMap();
            configuration.CreateMap<NlpWorkflowStateImExport, NlpWorkflowState>().ReverseMap();

            configuration.CreateMap<CreateOrEditNlpWorkflowDto, NlpWorkflow>().ReverseMap();
            configuration.CreateMap<NlpWorkflowDto, NlpWorkflow>().ReverseMap();
            configuration.CreateMap<NlpWorkflowImExport, NlpWorkflow>().ReverseMap();

            configuration.CreateMap<NlpFacebookUserDto, NlpFacebookUser>().ReverseMap();
            configuration.CreateMap<NlpLineUserDto, NlpLineUser>().ReverseMap();
            configuration.CreateMap<NlpClientInfoDto, NlpClientInfo>().ReverseMap();

            //configuration.CreateMap<CreateOrEditNlpQALibraryDto, NlpQALibrary>().ReverseMap();
            //configuration.CreateMap<NlpQALibraryDto, NlpQALibrary>().ReverseMap();
            //configuration.CreateMap<CreateOrEditNlpCbQAAccuracyDto, NlpCbQAAccuracy>().ReverseMap();
            configuration.CreateMap<NlpCbQAAccuracyDto, NlpCbQAAccuracy>().ReverseMap();
            //configuration.CreateMap<CreateOrEditNlpCbMessageDto, NlpCbMessage>().ReverseMap();
            configuration.CreateMap<NlpCbMessageDto, NlpCbMessage>().ReverseMap();
            configuration.CreateMap<CreateOrEditNlpCbTrainedAnswerDto, NlpCbTrainedAnswer>().ReverseMap();
            configuration.CreateMap<NlpCbTrainedAnswerDto, NlpCbTrainedAnswer>().ReverseMap();
            configuration.CreateMap<CreateOrEditNlpTokenDto, NlpToken>().ReverseMap();
            configuration.CreateMap<NlpTokenDto, NlpToken>().ReverseMap();

            //configuration.CreateMap<CreateOrEditNlpCbTrainingDataDto, NlpCbTrainingData>().ReverseMap();
            configuration.CreateMap<NlpCbTrainingDataDto, NlpCbTrainingData>().ReverseMap();
            configuration.CreateMap<CreateOrEditNlpCbModelDto, NlpCbModel>().ReverseMap();
            configuration.CreateMap<NlpCbModelDto, NlpCbModel>().ReverseMap();
            configuration.CreateMap<CreateOrEditNlpQADto, NlpQA>().ReverseMap();
            configuration.CreateMap<NlpQADto, NlpQA>().ReverseMap();
            configuration.CreateMap<Nlp.Dtos.NlpChatbot.ExportedImportedChatbotQAData, NlpQA>().ReverseMap();
            configuration.CreateMap<NlpQAImExport, NlpQA>().ReverseMap();

            configuration.CreateMap<CreateOrEditNlpChatbotDto, NlpChatbot>().ReverseMap();
            configuration.CreateMap<CreateOrEditNlpChatbotDto, NlpChatbotDto>().ReverseMap();
            configuration.CreateMap<Nlp.Dtos.NlpChatbot.ExportedImportedChatbotData, NlpChatbotDto>().ReverseMap();
            configuration.CreateMap<Nlp.Dtos.NlpChatbot.ExportedImportedChatbotData, NlpChatbot>().ReverseMap();
            configuration.CreateMap<NlpChatbotImExport, NlpChatbotDto>().ReverseMap();
            configuration.CreateMap<NlpChatbotImExport, NlpChatbot>().ReverseMap();
            configuration.CreateMap<NlpChatbotDto, NlpChatbot>().ReverseMap();
            //Inputs
            configuration.CreateMap<CheckboxInputType, FeatureInputTypeDto>();
            configuration.CreateMap<SingleLineStringInputType, FeatureInputTypeDto>();
            configuration.CreateMap<ComboboxInputType, FeatureInputTypeDto>();
            configuration.CreateMap<IInputType, FeatureInputTypeDto>()
                .Include<CheckboxInputType, FeatureInputTypeDto>()
                .Include<SingleLineStringInputType, FeatureInputTypeDto>()
                .Include<ComboboxInputType, FeatureInputTypeDto>();
            configuration.CreateMap<StaticLocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>();
            configuration.CreateMap<ILocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>()
                .Include<StaticLocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>();
            configuration.CreateMap<LocalizableComboboxItem, LocalizableComboboxItemDto>();
            configuration.CreateMap<ILocalizableComboboxItem, LocalizableComboboxItemDto>()
                .Include<LocalizableComboboxItem, LocalizableComboboxItemDto>();

            //Chat
            configuration.CreateMap<ChatMessage, ChatMessageDto>();
            configuration.CreateMap<ChatMessage, ChatMessageExportDto>();

            //Feature
            configuration.CreateMap<FlatFeatureSelectDto, Feature>().ReverseMap();
            configuration.CreateMap<Feature, FlatFeatureDto>();

            //Role
            configuration.CreateMap<RoleEditDto, Role>().ReverseMap();
            configuration.CreateMap<Role, RoleListDto>();
            configuration.CreateMap<UserRole, UserListRoleDto>();

            //Edition
            configuration.CreateMap<EditionEditDto, SubscribableEdition>().ReverseMap();
            configuration.CreateMap<EditionCreateDto, SubscribableEdition>();
            configuration.CreateMap<EditionSelectDto, SubscribableEdition>().ReverseMap();
            configuration.CreateMap<SubscribableEdition, EditionInfoDto>();

            configuration.CreateMap<Edition, EditionInfoDto>().Include<SubscribableEdition, EditionInfoDto>();

            configuration.CreateMap<SubscribableEdition, EditionListDto>();
            configuration.CreateMap<Edition, EditionEditDto>();
            configuration.CreateMap<Edition, SubscribableEdition>();
            configuration.CreateMap<Edition, EditionSelectDto>();


            //Payment
            configuration.CreateMap<SubscriptionPaymentDto, SubscriptionPayment>().ReverseMap();
            configuration.CreateMap<SubscriptionPaymentListDto, SubscriptionPayment>().ReverseMap();
            configuration.CreateMap<SubscriptionPayment, SubscriptionPaymentInfoDto>();

            //Permission
            configuration.CreateMap<Permission, FlatPermissionDto>();
            configuration.CreateMap<Permission, FlatPermissionWithLevelDto>();

            //Language
            configuration.CreateMap<ApplicationLanguage, ApplicationLanguageEditDto>();
            configuration.CreateMap<ApplicationLanguage, ApplicationLanguageListDto>();
            configuration.CreateMap<NotificationDefinition, NotificationSubscriptionWithDisplayNameDto>();
            configuration.CreateMap<ApplicationLanguage, ApplicationLanguageEditDto>()
                .ForMember(ldto => ldto.IsEnabled, options => options.MapFrom(l => !l.IsDisabled));

            //Tenant
            configuration.CreateMap<Tenant, RecentTenant>();
            configuration.CreateMap<Tenant, TenantLoginInfoDto>();
            configuration.CreateMap<Tenant, TenantListDto>();
            configuration.CreateMap<TenantEditDto, Tenant>().ReverseMap();
            configuration.CreateMap<CurrentTenantInfoDto, Tenant>().ReverseMap();

            //User
            configuration.CreateMap<User, UserEditDto>()
                .ForMember(dto => dto.Password, options => options.Ignore())
                .ReverseMap()
                .ForMember(user => user.Password, options => options.Ignore());
            configuration.CreateMap<User, UserLoginInfoDto>();
            configuration.CreateMap<User, UserListDto>();
            configuration.CreateMap<User, ChatUserDto>();
            configuration.CreateMap<User, OrganizationUnitUserListDto>();
            configuration.CreateMap<Role, OrganizationUnitRoleListDto>();
            configuration.CreateMap<CurrentUserProfileEditDto, User>().ReverseMap();
            configuration.CreateMap<UserLoginAttemptDto, UserLoginAttempt>().ReverseMap();
            configuration.CreateMap<ImportUserDto, User>();

            //AuditLog
            configuration.CreateMap<AuditLog, AuditLogListDto>();
            configuration.CreateMap<EntityChange, EntityChangeListDto>();
            configuration.CreateMap<EntityPropertyChange, EntityPropertyChangeDto>();

            //Friendship
            configuration.CreateMap<Friendship, FriendDto>();
            configuration.CreateMap<FriendCacheItem, FriendDto>();

            //OrganizationUnit
            configuration.CreateMap<OrganizationUnit, OrganizationUnitDto>();

            //Webhooks
            configuration.CreateMap<WebhookSubscription, GetAllSubscriptionsOutput>();
            configuration.CreateMap<WebhookSendAttempt, GetAllSendAttemptsOutput>()
                .ForMember(webhookSendAttemptListDto => webhookSendAttemptListDto.WebhookName,
                    options => options.MapFrom(l => l.WebhookEvent.WebhookName))
                .ForMember(webhookSendAttemptListDto => webhookSendAttemptListDto.Data,
                    options => options.MapFrom(l => l.WebhookEvent.Data));

            configuration.CreateMap<WebhookSendAttempt, GetAllSendAttemptsOfWebhookEventOutput>();

            configuration.CreateMap<DynamicProperty, DynamicPropertyDto>().ReverseMap();
            configuration.CreateMap<DynamicPropertyValue, DynamicPropertyValueDto>().ReverseMap();
            configuration.CreateMap<DynamicEntityProperty, DynamicEntityPropertyDto>()
                .ForMember(dto => dto.DynamicPropertyName,
                    options => options.MapFrom(entity => entity.DynamicProperty.DisplayName.IsNullOrEmpty() ? entity.DynamicProperty.PropertyName : entity.DynamicProperty.DisplayName));
            configuration.CreateMap<DynamicEntityPropertyDto, DynamicEntityProperty>();

            configuration.CreateMap<DynamicEntityPropertyValue, DynamicEntityPropertyValueDto>().ReverseMap();

            //User Delegations
            configuration.CreateMap<CreateUserDelegationDto, UserDelegation>();

            /* ADD YOUR OWN CUSTOM AUTOMAPPER MAPPINGS HERE */
        }
    }
}
