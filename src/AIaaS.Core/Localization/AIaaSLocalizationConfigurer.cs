using System.Reflection;
using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Reflection.Extensions;

namespace AIaaS.Localization
{
    public static class AIaaSLocalizationConfigurer
    {
        public static void Configure(ILocalizationConfiguration localizationConfiguration)
        {
            //AIaaS
            localizationConfiguration.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    AIaaSConsts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AIaaSLocalizationConfigurer).GetAssembly(),
                        "AIaaS.Localization.AIaaS"
                    )
                )
            );
        }

        public static void Configure2(ILocalizationConfiguration localizationConfiguration)
        {
            localizationConfiguration.Sources.Clear();

            //AIaaS
            localizationConfiguration.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    AIaaSConsts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AIaaSLocalizationConfigurer).GetAssembly(),
                        "AIaaS.Localization.AIaaS"
                    )
                )
            );

            //Abp
            localizationConfiguration.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    "Abp",
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AIaaSLocalizationConfigurer).GetAssembly(),
                        "AIaaS.Localization.Abp"
                    )
                )
            );

            //AbpWeb
            localizationConfiguration.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    "AbpWeb",
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AIaaSLocalizationConfigurer).GetAssembly(),
                        "AIaaS.Localization.AbpWeb"
                    )
                )
            );

            //AbpZero
            localizationConfiguration.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    "AbpZero",
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AIaaSLocalizationConfigurer).GetAssembly(),
                        "AIaaS.Localization.AbpZero"
                    )
                )
            );
        }
    }
}