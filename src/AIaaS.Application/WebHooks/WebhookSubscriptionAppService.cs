using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Localization;
using Abp.Webhooks;
using ApiProtectorDotNet;
using AIaaS.Authorization;
using AIaaS.WebHooks.Dto;

namespace AIaaS.WebHooks
{
    [AbpAuthorize(AppPermissions.Pages_Administration_WebhookSubscription)]
    public class WebhookSubscriptionAppService : AIaaSAppServiceBase, IWebhookSubscriptionAppService
    {
        private readonly IWebhookSubscriptionManager _webHookSubscriptionManager;
        private readonly IAppWebhookPublisher _appWebhookPublisher;
        private readonly IWebhookDefinitionManager _webhookDefinitionManager;
        private readonly ILocalizationContext _localizationContext;

        public WebhookSubscriptionAppService(
            IWebhookSubscriptionManager webHookSubscriptionManager,
            IAppWebhookPublisher appWebhookPublisher,
            IWebhookDefinitionManager webhookDefinitionManager,
            ILocalizationContext localizationContext
            )
        {
            _webHookSubscriptionManager = webHookSubscriptionManager;
            _appWebhookPublisher = appWebhookPublisher;
            _webhookDefinitionManager = webhookDefinitionManager;
            _localizationContext = localizationContext;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<string> PublishTestWebhook()
        {
            await _appWebhookPublisher.PublishTestWebhook();
            return L("WebhookSendAttemptInQueue") + "(" + L("YouHaveToSubscribeToTestWebhookToReceiveTestEvent") + ")";
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ListResultDto<GetAllSubscriptionsOutput>> GetAllSubscriptions()
        {
            var subscriptions = await _webHookSubscriptionManager.GetAllSubscriptionsAsync(AbpSession.TenantId);
            return new ListResultDto<GetAllSubscriptionsOutput>(
                ObjectMapper.Map<List<GetAllSubscriptionsOutput>>(subscriptions)
                );
        }

        [AbpAuthorize(
            AppPermissions.Pages_Administration_WebhookSubscription_Create,
            AppPermissions.Pages_Administration_WebhookSubscription_Edit,
            AppPermissions.Pages_Administration_WebhookSubscription_Detail
            )
        ]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<WebhookSubscription> GetSubscription(string subscriptionId)
        {
            return await _webHookSubscriptionManager.GetAsync(Guid.Parse(subscriptionId));
        }

        [AbpAuthorize(AppPermissions.Pages_Administration_WebhookSubscription_Create)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task AddSubscription(WebhookSubscription subscription)
        {
            subscription.TenantId = AbpSession.TenantId;

            await _webHookSubscriptionManager.AddOrUpdateSubscriptionAsync(subscription);
        }

        [AbpAuthorize(AppPermissions.Pages_Administration_WebhookSubscription_Edit)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task UpdateSubscription(WebhookSubscription subscription)
        {
            if (subscription.Id == default)
            {
                throw new ArgumentNullException(nameof(subscription.Id));
            }

            subscription.TenantId = AbpSession.TenantId;

            await _webHookSubscriptionManager.AddOrUpdateSubscriptionAsync(subscription);
        }

        [AbpAuthorize(AppPermissions.Pages_Administration_WebhookSubscription_ChangeActivity)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task ActivateWebhookSubscription(ActivateWebhookSubscriptionInput input)
        {
            await _webHookSubscriptionManager.ActivateWebhookSubscriptionAsync(input.SubscriptionId, input.IsActive);
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<bool> IsSubscribed(string webhookName)
        {
            return await _webHookSubscriptionManager.IsSubscribedAsync(AbpSession.TenantId, webhookName);
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ListResultDto<GetAllSubscriptionsOutput>> GetAllSubscriptionsIfFeaturesGranted(string webhookName)
        {
            var subscriptions = await _webHookSubscriptionManager.GetAllSubscriptionsIfFeaturesGrantedAsync(AbpSession.TenantId, webhookName);
            return new ListResultDto<GetAllSubscriptionsOutput>(
                ObjectMapper.Map<List<GetAllSubscriptionsOutput>>(subscriptions)
            );
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ListResultDto<GetAllAvailableWebhooksOutput>> GetAllAvailableWebhooks()
        {
            var webhooks = _webhookDefinitionManager.GetAll();
            var definitions = new List<GetAllAvailableWebhooksOutput>();

            foreach (var webhookDefinition in webhooks)
            {
                if (await _webhookDefinitionManager.IsAvailableAsync(AbpSession.TenantId, webhookDefinition.Name))
                {
                    definitions.Add(new GetAllAvailableWebhooksOutput()
                    {
                        Name = webhookDefinition.Name,
                        Description = webhookDefinition.Description?.Localize(_localizationContext),
                        DisplayName = webhookDefinition.DisplayName?.Localize(_localizationContext)
                    });
                }
            }

            return new ListResultDto<GetAllAvailableWebhooksOutput>(definitions.OrderBy(d => d.Name).ToList());
        }
    }
}
