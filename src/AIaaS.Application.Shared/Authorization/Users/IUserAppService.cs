﻿using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Authorization.Users.Dto;
using AIaaS.Dto;
using AIaaS.License;

namespace AIaaS.Authorization.Users
{
    public interface IUserAppService : IApplicationService
    {
        Task<PagedResultDto<UserListDto>> GetUsers(GetUsersInput input);

        Task<FileDto> GetUsersToExcel(GetUsersToExcelInput input);

        Task<GetUserForEditOutput> GetUserForEdit(NullableIdDto<long> input);

        Task<GetUserPermissionsForEditOutput> GetUserPermissionsForEdit(EntityDto<long> input);

        Task ResetUserSpecificPermissions(EntityDto<long> input);

        Task UpdateUserPermissions(UpdateUserPermissionsInput input);

        Task CreateOrUpdateUser(CreateOrUpdateUserInput input);

        Task DeleteUser(EntityDto<long> input);

        Task UnlockUser(EntityDto<long> input);
        Task<LicenseUsage> GetUserLicenseUsage();
    }
}