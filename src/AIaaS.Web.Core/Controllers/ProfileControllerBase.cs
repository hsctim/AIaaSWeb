using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetZeroCore.Net;
using Abp.Extensions;
using Abp.IO.Extensions;
using Abp.UI;
using Abp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization.Users.Profile;
using AIaaS.Authorization.Users.Profile.Dto;
using AIaaS.Dto;
using AIaaS.Graphics;
using AIaaS.Storage;
using SkiaSharp;

namespace AIaaS.Web.Controllers
{
    public abstract class ProfileControllerBase : AIaaSControllerBase
    {
        private readonly ITempFileCacheManager _tempFileCacheManager;
        private readonly IProfileAppService _profileAppService;
        private readonly IImageFormatValidator _imageFormatValidator;
        
        private const int MaxProfilePictureSize = 1024*1024; //1MB

        protected ProfileControllerBase(
            ITempFileCacheManager tempFileCacheManager, 
            IProfileAppService profileAppService, 
            IImageFormatValidator imageFormatValidator)
        {
            _tempFileCacheManager = tempFileCacheManager;
            _profileAppService = profileAppService;
            _imageFormatValidator = imageFormatValidator;
        }

        public UploadProfilePictureOutput UploadProfilePicture(FileDto input)
        {
            try
            {
                var profilePictureFile = Request.Form.Files.First();

                //Check input
                if (profilePictureFile == null)
                {
                    throw new UserFriendlyException(L("ProfilePicture_Change_Error"));
                }

                if (profilePictureFile.Length > MaxProfilePictureSize)
                {
                    throw new UserFriendlyException(L("ProfilePicture_Warn_SizeLimit",
                        AppConsts.MaxProfilePictureBytesUserFriendlyValue));
                }

                byte[] fileBytes;
                SKImage sKImage = null;
                using (var stream = profilePictureFile.OpenReadStream())
                {
                    fileBytes = stream.GetAllBytes();
                    sKImage=_imageFormatValidator.Validate(fileBytes);

                }

                _tempFileCacheManager.SetFile(input.FileToken, fileBytes);

                return new UploadProfilePictureOutput
                {
                    FileToken = input.FileToken,
                    FileName = input.FileName,
                    FileType = input.FileType,
                    Width = sKImage.Width,
                    Height = sKImage.Height
                };
            }
            catch (UserFriendlyException ex)
            {
                return new UploadProfilePictureOutput(new ErrorInfo(ex.Message));
            }
        }

        [AllowAnonymous]
        public FileResult GetDefaultProfilePicture()
        {
            return GetDefaultProfilePictureInternal();
        }

        public async Task<FileResult> GetProfilePictureByUser(long userId)
        {
            var output = await _profileAppService.GetProfilePictureByUser(userId);
            if (output.ProfilePicture.IsNullOrEmpty())
            {
                return GetDefaultProfilePictureInternal();
            }

            return File(Convert.FromBase64String(output.ProfilePicture), MimeTypeNames.ImageJpeg);
        }
        
        protected FileResult GetDefaultProfilePictureInternal()
        {
            return File(Path.Combine("Common", "Images", "default-profile-picture.png"), MimeTypeNames.ImagePng);
        }
    }
}