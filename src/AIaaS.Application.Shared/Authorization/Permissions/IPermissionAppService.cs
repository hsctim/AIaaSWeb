using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Authorization.Permissions.Dto;

namespace AIaaS.Authorization.Permissions
{
    public interface IPermissionAppService : IApplicationService
    {
        ListResultDto<FlatPermissionWithLevelDto> GetAllPermissions();
    }
}
