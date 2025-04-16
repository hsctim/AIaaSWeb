using System.Threading.Tasks;
using Abp.Webhooks;

namespace AIaaS.WebHooks
{
    public interface IWebhookEventAppService
    {
        Task<WebhookEvent> Get(string id);
    }
}
