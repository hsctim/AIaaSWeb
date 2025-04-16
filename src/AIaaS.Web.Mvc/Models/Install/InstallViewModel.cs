using System.Collections.Generic;
using Abp.Localization;
using AIaaS.Install.Dto;

namespace AIaaS.Web.Models.Install
{
    public class InstallViewModel
    {
        public List<ApplicationLanguage> Languages { get; set; }

        public AppSettingsJsonDto AppSettingsJson { get; set; }
    }
}
