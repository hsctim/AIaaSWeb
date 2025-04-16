using Abp.Authorization;
using AIaaS.Authorization.Roles;
using AIaaS.Authorization.Users;

namespace AIaaS.Authorization
{
    public class PermissionChecker : PermissionChecker<Role, User>
    {
        public PermissionChecker(UserManager userManager)
            : base(userManager)
        {

        }
    }
}
