using System.Threading.Tasks;
using AIaaS.Authorization.Users;

namespace AIaaS.WebHooks
{
    public interface IAppWebhookPublisher
    {
        Task PublishTestWebhook();
    }
}
