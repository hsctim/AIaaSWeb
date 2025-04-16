using Abp.AspNetCore.Mvc.Authorization;
using AIaaS.Authorization.Users.Profile;
using AIaaS.Graphics;
using AIaaS.Storage;

namespace AIaaS.Web.Controllers
{
    [AbpMvcAuthorize]
    public class ProfileController : ProfileControllerBase
    {
        public ProfileController(
            ITempFileCacheManager tempFileCacheManager,
            IProfileAppService profileAppService,
            IImageFormatValidator imageFormatValidator) :
            base(tempFileCacheManager, profileAppService, imageFormatValidator)
        {
        }
    }
}