using System;
using System.Threading.Tasks;
using Abp.Net.Mail;
using Abp.UI;
using Microsoft.Extensions.Configuration;
using AIaaS.Configuration.Dto;
using AIaaS.Configuration.Host.Dto;
using ApiProtectorDotNet;

namespace AIaaS.Configuration
{
    public abstract class SettingsAppServiceBase : AIaaSAppServiceBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IAppConfigurationAccessor _configurationAccessor;

        protected SettingsAppServiceBase(
            IEmailSender emailSender,
            IAppConfigurationAccessor configurationAccessor)
        {
            _emailSender = emailSender;
            _configurationAccessor = configurationAccessor;
        }

        #region Send Test Email



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task SendTestEmail(SendTestEmailInput input)
        {
            try
            {
                await _emailSender.SendAsync(
                    input.EmailAddress,
                    L("TestEmail_Subject"),
                    L("TestEmail_Body")
                );
            }
            catch (Exception e)
            {
                throw new UserFriendlyException("An error was encountered while sending an email. " + e.Message, e);
            }
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public ExternalLoginSettingsDto GetEnabledSocialLoginSettings()
        {
            var dto = new ExternalLoginSettingsDto();
            if (!bool.Parse(_configurationAccessor.Configuration["Authentication:AllowSocialLoginSettingsPerTenant"]))
            {
                return dto;
            }

            if (IsSocialLoginEnabled("Facebook"))
            {
                dto.EnabledSocialLoginSettings.Add("Facebook");
            }

            if (IsSocialLoginEnabled("Google"))
            {
                dto.EnabledSocialLoginSettings.Add("Google");
            }

            if (IsSocialLoginEnabled("Twitter"))
            {
                dto.EnabledSocialLoginSettings.Add("Twitter");
            }

            if (IsSocialLoginEnabled("Microsoft"))
            {
                dto.EnabledSocialLoginSettings.Add("Microsoft");
            }

            if (IsSocialLoginEnabled("WsFederation"))
            {
                dto.EnabledSocialLoginSettings.Add("WsFederation");
            }

            if (IsSocialLoginEnabled("OpenId"))
            {
                dto.EnabledSocialLoginSettings.Add("OpenId");
            }

            return dto;
        }

        private bool IsSocialLoginEnabled(string name)
        {
            return _configurationAccessor.Configuration.GetSection("Authentication:" + name).Exists() &&
                   bool.Parse(_configurationAccessor.Configuration["Authentication:" + name + ":IsEnabled"]);
        }

        #endregion
    }
}