using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AIaaS.Web.Views.Shared.Components.AccountLanguages
{
    public class AccountLanguagesViewComponent : AIaaSViewComponent
    {
        private readonly ILanguageManager _languageManager;

        public AccountLanguagesViewComponent(ILanguageManager languageManager)
        {
            _languageManager = languageManager;
        }

        public Task<IViewComponentResult> InvokeAsync()
        {
            var currentLanguage = _languageManager.CurrentLanguage;

            var languages = from e in _languageManager.GetActiveLanguages()
                            orderby e.Name != currentLanguage.Name, e.Name
                            select new SelectListItem()
                            {
                                Text = e.DisplayName,
                                Value = e.Name,
                            };


            var model = new LanguageSelectionViewModel
            {
                CurrentLanguage = currentLanguage,
                //Languages = _languageManager.GetActiveLanguages().OrderBy(e => e.Name).ToList(),
                CurrentUrl = Request.Path,
                LanguageSelectList = new SelectList(languages, "Value", "Text", currentLanguage.Name),
            };

            return Task.FromResult(View(model) as IViewComponentResult);
        }
    }
}
