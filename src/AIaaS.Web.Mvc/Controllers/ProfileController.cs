using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.AspNetZeroCore.Net;
using Abp.Auditing;
using Abp.Extensions;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization.Users.Profile;
using AIaaS.Authorization.Users.Profile.Dto;
using AIaaS.Graphics;
using AIaaS.Storage;
using AIaaS.Helpers;
using Abp.Threading;
using Abp.Runtime.Caching;
using Abp.Authorization;
using ApiProtectorDotNet;
using Abp.Timing;

namespace AIaaS.Web.Controllers
{
    [AbpMvcAuthorize]
    [DisableAuditing]
    public class ProfileController : ProfileControllerBase
    {
        private readonly IProfileAppService _profileAppService;
        //private readonly NlpCacheManagerHelper _nlpLRUCacheHelper;
        private readonly IBinaryObjectManager _binaryObjectManager;
        private readonly ICacheManager _cacheManager;

        public ProfileController(
            ITempFileCacheManager tempFileCacheManager,
            IProfileAppService profileAppService,
            IImageFormatValidator imageFormatValidator,
            IBinaryObjectManager binaryObjectManager,
            //NlpCacheManagerHelper nlpLRUCacheHelper
            ICacheManager cacheManager
            ) : base(tempFileCacheManager, profileAppService, imageFormatValidator)
        {
            _profileAppService = profileAppService;
            _cacheManager = cacheManager;
            _binaryObjectManager = binaryObjectManager;
        }

        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        [DisableAuditing]
        public async Task<FileResult> GetProfilePicture()
        {
            FileResult fileResult = (FileResult)_cacheManager.Get_UserProfilePicture(AbpSession.UserId.Value);

            if (fileResult != null)
                return fileResult;

            var output = await _profileAppService.GetProfilePicture();

            if (output.ProfilePicture.IsNullOrEmpty())
            {
                return GetDefaultProfilePictureInternal();
            }

            fileResult = File(Convert.FromBase64String(output.ProfilePicture), MimeTypeNames.ImageJpeg);
            fileResult.LastModified = Clock.Now;
            _cacheManager.Set_UserProfilePicture(AbpSession.UserId.Value, fileResult);


            return fileResult;
        }

        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        [DisableAuditing]
        public async Task<FileResult> GetProfilePictureById(Guid? id)
        {
            if (id == null)
                return await GetProfilePicture();

            FileResult fileResult = (FileResult)_cacheManager.Get_UserProfilePicture_By_PicId(id.Value);

            if (fileResult != null)
                return fileResult;

            var data = await _binaryObjectManager.GetOrNullAsync(id.Value);
            if (data == null)
                return await GetProfilePicture();

            fileResult = File(data.Bytes, MimeTypeNames.ImageJpeg);
            fileResult.LastModified = Clock.Now;

            _cacheManager.Set_UserProfilePicture_By_PicId(id.Value, fileResult);

            return fileResult;
        }




        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public virtual async Task<FileResult> GetFriendProfilePicture(long userId, int? tenantId)
        {
            var output = await _profileAppService.GetFriendProfilePicture(new GetFriendProfilePictureInput()
            {
                TenantId = tenantId,
                UserId = userId
            });

            if (output.ProfilePicture.IsNullOrEmpty())
            {
                return GetDefaultProfilePictureInternal();
            }

            return File(Convert.FromBase64String(output.ProfilePicture), MimeTypeNames.ImageJpeg);
        }
    }
}
