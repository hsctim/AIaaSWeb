using System.Threading.Tasks;
using Abp.Application.Services;
using AIaaS.Install.Dto;

namespace AIaaS.Install
{
    public interface IInstallAppService : IApplicationService
    {
        Task Setup(InstallDto input);

        AppSettingsJsonDto GetAppSettingsJson();

        CheckDatabaseOutput CheckDatabase();
    }
}