using Abp.Application.Services;
using AIaaS.Dto;
using AIaaS.Logging.Dto;

namespace AIaaS.Logging
{
    public interface IWebLogAppService : IApplicationService
    {
        GetLatestWebLogsOutput GetLatestWebLogs();

        FileDto DownloadWebLogs();
    }
}
