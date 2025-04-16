using System.Threading.Tasks;
using Abp.Application.Services;
using AIaaS.Configuration.Host.Dto;

namespace AIaaS.Configuration.Host
{
    public interface IHostSettingsAppService : IApplicationService
    {
        Task<HostSettingsEditDto> GetAllSettings();

        Task UpdateAllSettings(HostSettingsEditDto input);

        Task SendTestEmail(SendTestEmailInput input);
    }
}
