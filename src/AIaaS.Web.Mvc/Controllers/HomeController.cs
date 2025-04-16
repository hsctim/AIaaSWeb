using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Identity;
using ApiProtectorDotNet;
using Abp.Localization;
using System.Linq;
using System;
using AIaaS.Configuration;
using Microsoft.Extensions.Configuration;

namespace AIaaS.Web.Controllers
{
    public class HomeController : AIaaSControllerBase
    {        
        private readonly SignInManager _signInManager;
        private readonly ILanguageManager _languageManager;
        private readonly IApplicationLanguageManager _applicationLanguageManager;
        private readonly IConfigurationRoot _appConfiguration;

        private static bool _initialled = false;

        public HomeController(SignInManager signInManager,
            ILanguageManager languageManager,
            IApplicationLanguageManager applicationLanguageManager,
			IAppConfigurationAccessor configurationAccessor)
        {
            _signInManager = signInManager;
            _languageManager = languageManager;
            _applicationLanguageManager = applicationLanguageManager;
            _appConfiguration = configurationAccessor.Configuration;
        }


        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> Index(string redirect = "", bool forceNewRegistration = false)
        {
            if (string.Compare(Request.Host.Host, "www.qabot.ai", true) == 0 ||
                string.Compare(Request.Host.Host, "www.chatpal.ai", true) == 0 ||
                string.Compare(Request.Host.Host, "qabotai.azurewebsites.net", true) == 0)
            {
                var url = $"{Request.Scheme}://app.chatpal.ai{Request.PathBase.Value}/";
                return Redirect(url);
            }

            if (_initialled == false || Nlp.NlpChatbotHelper.HostChatbotId == Guid.Empty)
            {
                _initialled = true;
                var guidStr = _appConfiguration["NlpChatbot:HostChatbotId"];
                _ = Guid.TryParse(guidStr, out Nlp.NlpChatbotHelper.HostChatbotId);
            }


            if (forceNewRegistration)
            {
                await _signInManager.SignOutAsync();
            }

            if (redirect == "TenantRegistration")
            {
                return RedirectToAction("SelectEdition", "TenantRegistration");
            }

            try
            {
                var currentLanguage = _languageManager.CurrentLanguage;

                //Logger.Error("currentLanguage.Name: " + currentLanguage.Name);
                //Logger.Error("currentLanguage.IsDisabled: " + currentLanguage.IsDisabled.ToString());

                if (currentLanguage.IsDisabled == true)
                {
                    currentLanguage = _languageManager.GetActiveLanguages().OrderByDescending(e => e.Name).First();

                    //Logger.Error("new currentLanguage.Name: " + currentLanguage.Name);
                    //Logger.Error("new currentLanguage.IsDisabled: " + currentLanguage.IsDisabled.ToString());

                    await _applicationLanguageManager.SetDefaultLanguageAsync(AbpSession.TenantId, currentLanguage.Name);

                    return RedirectToAction("ChangeCulture", "AbpLocalization",
                        new
                        {
                            cultureName = currentLanguage.Name,
                            returnUrl = "/Home",
                            redirect = redirect
                        });
                }
            }
            catch (System.Exception ex)
            {
                Logger.Fatal(ex.Message, ex);
            }

            return AbpSession.UserId.HasValue ?
                RedirectToAction("Index", "Home", new { area = "App" }) :
                RedirectToAction("Login", "Account");
        }
    }
}