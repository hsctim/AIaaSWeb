using Abp.Application.Services;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Auditing;
using Abp.Localization;
using AIaaS.Web.Controllers;
using AIaaS.Web.Models.Document;
using ApiProtectorDotNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize]
    [DisableAuditing]
    [RemoteService(false, IsMetadataEnabled = false)]
    public class DocumentController : AIaaSControllerBase
    {
        private readonly ILanguageManager _languageManager;
        private readonly IWebHostEnvironment _env;

        public DocumentController(ILanguageManager languageManager, IWebHostEnvironment env)
        {
            _languageManager = languageManager;
            _env = env;
        }


        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        [Route("App/Document/{Document}")]
        public ActionResult Index(string Document)
        {
            // 這是安全性檢查，不能刪除
            if (Document.Contains("..") || Document.Contains('\\') || Document.Contains('/'))
                return NotFound();

            return View(new DocumentViewModel()
            {
                Language = _languageManager.CurrentLanguage.Name,
                Document = Document,
                ContentRootPath = _env.ContentRootPath
            });
        }


        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        [Route("App/Document/{Language}/{Document}")]
        [DisableAuditing]
        //[ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        public ActionResult Index(string Language, string Document)
        {
            // 這是安全性檢查，不能刪除
            var s = Language + Document;
            if (s.Contains('~') || s.Contains('.') || s.Contains('\\') || s.Contains('/') || s.Contains('?'))
                return NotFound();

            return View(new DocumentViewModel()
            {
                Language = Language,
                Document = Document,
                ContentRootPath = _env.ContentRootPath
            });
        }
    }
}