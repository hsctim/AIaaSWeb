using Abp.Application.Services.Dto;
using Abp.Webhooks;
using AIaaS.WebHooks.Dto;

namespace AIaaS.Web.Areas.App.Models.Webhooks
{
    public class CreateOrEditWebhookSubscriptionViewModel
    {
        public WebhookSubscription WebhookSubscription { get; set; }

        public ListResultDto<GetAllAvailableWebhooksOutput> AvailableWebhookEvents { get; set; }
    }
}
