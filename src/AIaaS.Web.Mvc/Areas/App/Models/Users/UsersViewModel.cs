using System.Collections.Generic;
using Abp.Application.Services.Dto;
using AIaaS.Authorization.Permissions.Dto;
using AIaaS.License;
using AIaaS.Web.Areas.App.Models.Common;

namespace AIaaS.Web.Areas.App.Models.Users
{
    public class UsersViewModel : IPermissionsEditViewModel
    {
        public string FilterText { get; set; }

        public List<ComboboxItemDto> Roles { get; set; }

        public bool OnlyLockedUsers { get; set; }

        public List<FlatPermissionDto> Permissions { get; set; }

        public List<string> GrantedPermissionNames { get; set; }

        public string WarningMessage { get; set; }

        public LicenseUsage Usage { get; set; }
    }
}
