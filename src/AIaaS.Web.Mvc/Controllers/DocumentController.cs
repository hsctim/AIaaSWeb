using Abp.Application.Services;
using Abp.Auditing;
using Abp.Localization;
using AIaaS.Web.Models.Document;
using ApiProtectorDotNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;


namespace AIaaS.Web.Controllers
{
    [DisableAuditing]
    [RemoteService(false)]
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
        [Route("Document/{Document}")]
        public ActionResult Index(string Document)
        {
            // 這是安全性檢查，不能刪除
            if (Document.Contains('~') || Document.Contains('.') || Document.Contains('\\') || Document.Contains('/') || Document.Contains('?'))
                return NotFound();

            return View(new DocumentViewModel()
            {
                Language = _languageManager.CurrentLanguage.Name,
                Document = Document,
                ContentRootPath = _env.ContentRootPath
            });
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        [Route("Document/{Language}/{Document}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        [DisableAuditing]
        public ActionResult Index(string Language, string Document)
        {
            // 這是安全性檢查，不能刪除
            if (Document.Contains("..") || Document.Contains('\\') || Document.Contains('/') ||
                Language.Contains("..") || Language.Contains('\\') || Language.Contains('/'))
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