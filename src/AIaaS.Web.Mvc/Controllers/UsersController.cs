using Abp.AspNetCore.Mvc.Authorization;
using AIaaS.Authorization;
using AIaaS.Storage;
using Abp.BackgroundJobs;
using Abp.Authorization;

namespace AIaaS.Web.Controllers
{
    [AbpMvcAuthorize(AppPermissions.Pages_Administration_Users)]
    public class UsersController : UsersControllerBase
    {
        public UsersController(IBinaryObjectManager binaryObjectManager, IBackgroundJobManager backgroundJobManager)
            : base(binaryObjectManager, backgroundJobManager)
        {
        }
    }
}