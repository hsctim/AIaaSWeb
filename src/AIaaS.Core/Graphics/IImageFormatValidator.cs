using System.IO;
using Abp.Extensions;
using Abp.UI;
using SkiaSharp;

namespace AIaaS.Graphics
{
    public interface IImageFormatValidator
    {
        SKImage Validate(byte[] imageBytes);
    }

    public class SkiaSharpImageFormatValidator : AIaaSDomainServiceBase, IImageFormatValidator
    {
        public SKImage Validate(byte[] imageBytes)
        {
            var skImage = SKImage.FromEncodedData(imageBytes);
            
            if (skImage == null)
            {
                throw new UserFriendlyException(L("IncorrectImageFormat"));
            }

            return skImage;
        }
    }
}