using Abp.Application.Features;
using Abp.Application.Services.Dto;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Abp.Zero.Configuration;
using AIaaS.Configuration;
using AIaaS.Debugging;
using AIaaS.Editions;
using AIaaS.Editions.Dto;
using AIaaS.Features;
using AIaaS.MultiTenancy.Dto;
using AIaaS.MultiTenancy.Payments.Dto;
using AIaaS.Notifications;
using AIaaS.Security.Recaptcha;
using AIaaS.Url;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIaaS.MultiTenancy.Payments;
using ApiProtectorDotNet;
using Abp.Domain.Repositories;
using System.Text.RegularExpressions;
using AIaaS.Authorization.Users;
using Microsoft.EntityFrameworkCore;
using Abp.Domain.Uow;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace AIaaS.MultiTenancy
{
    public class TenantRegistrationAppService : AIaaSAppServiceBase, ITenantRegistrationAppService
    {
        public IAppUrlService AppUrlService { get; set; }

        private readonly IMultiTenancyConfig _multiTenancyConfig;
        private readonly IRecaptchaValidator _recaptchaValidator;
        private readonly EditionManager _editionManager;
        private readonly IAppNotifier _appNotifier;
        private readonly ILocalizationContext _localizationContext;
        private readonly TenantManager _tenantManager;
        private readonly ISubscriptionPaymentRepository _subscriptionPaymentRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IEditionAppService _editionAppService;
        private readonly IRepository<Tenant> _tenantRepository;

        private readonly IAppConfigurationAccessor _configurationAccessor;


        public TenantRegistrationAppService(
            IMultiTenancyConfig multiTenancyConfig,
            IRecaptchaValidator recaptchaValidator,
            EditionManager editionManager,
            IAppNotifier appNotifier,
            ILocalizationContext localizationContext,
            TenantManager tenantManager,
            ISubscriptionPaymentRepository subscriptionPaymentRepository,
            IRepository<User, long> userRepository,
            IEditionAppService editionAppService,
            IRepository<Tenant> tenantRepository,
            IAppConfigurationAccessor configurationAccessor
            )
        {
            _multiTenancyConfig = multiTenancyConfig;
            _recaptchaValidator = recaptchaValidator;
            _editionManager = editionManager;
            _appNotifier = appNotifier;
            _localizationContext = localizationContext;
            _tenantManager = tenantManager;
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _userRepository = userRepository;
            _editionAppService = editionAppService;
            _tenantRepository = tenantRepository;
            _configurationAccessor = configurationAccessor;

            AppUrlService = NullAppUrlService.Instance;
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<RegisterTenantOutput> RegisterTenant(RegisterTenantInput input)
        {
            if (input.SubscriptionStartType == SubscriptionStartType.Paid)
                throw new UserFriendlyException(L("SelfTenantRegistrationPaidIsDisabledMessage_Detail"));

            CheckTenantName(input.TenancyName);

            if (input.EditionId.HasValue)
            {
                await CheckEditionSubscriptionAsync(input.EditionId.Value, input.SubscriptionStartType);
            }
            else
            {
                await CheckRegistrationWithoutEdition();
            }

            if (input.SubscriptionStartType == SubscriptionStartType.Free || input.SubscriptionStartType == SubscriptionStartType.Trial)
                await CheckTooManyFreeAccount(input.AdminEmailAddress);

            using (CurrentUnitOfWork.SetTenantId(null))
            {
                CheckTenantRegistrationIsEnabled();

                if (UseCaptchaOnRegistration())
                {
                    await _recaptchaValidator.ValidateAsync(input.CaptchaResponse);
                }

                //Getting host-specific settings
                var isActive = await IsNewRegisteredTenantActiveByDefault(input.SubscriptionStartType);
                var isEmailConfirmationRequired = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                    AbpZeroSettingNames.UserManagement.IsEmailConfirmationRequiredForLogin
                );

                //要Email確認
                if (isEmailConfirmationRequired == false)
                    throw new UserFriendlyException("Error");

                DateTime? subscriptionEndDate = null;
                var isInTrialPeriod = false;

                if (input.EditionId.HasValue)
                {
                    isInTrialPeriod = input.SubscriptionStartType == SubscriptionStartType.Trial;

                    if (isInTrialPeriod)
                    {
                        var edition = (SubscribableEdition)await _editionManager.GetByIdAsync(input.EditionId.Value);
                        subscriptionEndDate = Clock.Now.AddDays(edition.TrialDayCount ?? 0);
                    }
                }

                var tenantId = await _tenantManager.CreateWithAdminUserAsync(
                    input.TenancyName,
                    input.Name,
                    input.AdminPassword,
                    input.AdminEmailAddress,
                    null,
                    isActive,
                    input.EditionId,
                    shouldChangePasswordOnNextLogin: false,
                    sendActivationEmail: true,
                    subscriptionEndDate,
                    isInTrialPeriod,
                    AppUrlService.CreateEmailActivationUrlFormat(input.TenancyName),
                    adminName: input.AdminName,
                    adminSurname: input.AdminSurname
                );

                var tenant = await TenantManager.GetByIdAsync(tenantId);
                await _appNotifier.NewTenantRegisteredAsync(tenant);

                return new RegisterTenantOutput
                {
                    TenantId = tenant.Id,
                    TenancyName = input.TenancyName,
                    Name = input.Name,
                    UserName = AbpUserBase.AdminUserName,
                    EmailAddress = input.AdminEmailAddress,
                    IsActive = tenant.IsActive,
                    IsEmailConfirmationRequired = isEmailConfirmationRequired,
                    IsTenantActive = tenant.IsActive
                };
            }
        }

        private void CheckTenantName(string tenantName)
        {
            var tenantNamesSection = _configurationAccessor.Configuration.GetSection("App:ReservedTenantName");
            var tenantNames = tenantNamesSection.Get<string[]>();

            if (tenantNames == null || tenantNames.Length == 0)
            {
                throw new UserFriendlyException("Reserved tenant names configuration is missing or empty.");
            }

            // Compare tenant name with reserved names in a case-insensitive manner
            foreach (var name in tenantNames)
            {
                if (string.Equals(tenantName.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    // Throw exception if the tenant name is reserved
                    throw new UserFriendlyException(string.Format(L("TenancyNameIsAlreadyTaken"), tenantName));
                }
            }
        }

        private async Task<bool> IsNewRegisteredTenantActiveByDefault(SubscriptionStartType subscriptionStartType)
        {
            if (subscriptionStartType == SubscriptionStartType.Paid)
            {
                return false;
            }

            return await SettingManager.GetSettingValueForApplicationAsync<bool>(AppSettings.TenantManagement
                .IsNewRegisteredTenantActiveByDefault);
        }

        private async Task CheckRegistrationWithoutEdition()
        {
            Debugger.Break();
            var editions = (await _editionManager.GetAllAsync()).Where(e => e.Name == "Free" || e.Name == "Basic" || e.Name == "Pro" || e.Name == "Business");
            if (editions.Any())
            {
                throw new Exception(
                    "Tenant registration is not allowed without edition because there are editions defined !");
            }
        }

        public async Task<EditionsSelectOutput> GetEditionsForSelect()
        {
            var features = FeatureManager
                .GetAll()
                .Where(feature =>
                    (feature[FeatureMetadata.CustomFeatureKey] as FeatureMetadata)?.IsVisibleOnPricingTable ?? false);

            var flatFeatures = ObjectMapper
                .Map<List<FlatFeatureSelectDto>>(features)
                .OrderBy(f => f.DisplayName)
                .ToList();

            var editions = (await _editionManager.GetAllAsync())
                .Where(e => e.Name == "Free" || e.Name == "Basic" || e.Name == "Pro" || e.Name == "Business")
                .Cast<SubscribableEdition>()
                .OrderBy(e => e.MonthlyPrice)
                .ToList();


            var featureDictionary = features.ToDictionary(feature => feature.Name, f => f);

            var editionWithFeatures = new List<EditionWithFeaturesDto>();
            foreach (var edition in editions)
            {
                editionWithFeatures.Add(await CreateEditionWithFeaturesDto(edition, featureDictionary));
            }

            if (AbpSession.UserId.HasValue)
            {
                var currentEditionId = (await _tenantManager.GetByIdAsync(AbpSession.GetTenantId()))
                        .EditionId;

                if (currentEditionId.HasValue)
                {
                    editionWithFeatures = editionWithFeatures.Where(e => e.Edition.Id != currentEditionId).ToList();

                    var currentEdition =
                        (SubscribableEdition)(await _editionManager.GetByIdAsync(currentEditionId.Value));
                    if (!currentEdition.IsFree)
                    {
                        //var lastPayment = await _subscriptionPaymentRepository.GetLastCompletedPaymentOrDefaultAsync(
                        //    AbpSession.GetTenantId(),
                        //    null,
                        //    null);

                        editionWithFeatures = editionWithFeatures
                            .Where(e => e.Edition.GetPaymentAmount(PaymentPeriodType.Annual) >
                                    currentEdition.GetPaymentAmount(PaymentPeriodType.Annual)
                                ).ToList();
                    }
                }
            }

            return new EditionsSelectOutput
            {
                AllFeatures = flatFeatures,
                EditionsWithFeatures = editionWithFeatures,
            };
        }

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<EditionSelectDto> GetEdition(int editionId)
        {
            var edition = await _editionManager.GetByIdAsync(editionId);
            var editionDto = ObjectMapper.Map<EditionSelectDto>(edition);

            return editionDto;
        }

        private async Task<EditionWithFeaturesDto> CreateEditionWithFeaturesDto(SubscribableEdition edition,
            Dictionary<string, Feature> featureDictionary)
        {
            return new EditionWithFeaturesDto
            {
                Edition = ObjectMapper.Map<EditionSelectDto>(edition),
                FeatureValues = (await _editionManager.GetFeatureValuesAsync(edition.Id))
                    .Where(featureValue => featureDictionary.ContainsKey(featureValue.Name))
                    .Select(fv => new NameValueDto(
                        fv.Name,
                        featureDictionary[fv.Name].GetValueText(fv.Value, _localizationContext))
                    )
                    .ToList()
            };
        }

        private void CheckTenantRegistrationIsEnabled()
        {
            if (!IsSelfRegistrationEnabled())
            {
                throw new UserFriendlyException(L("SelfTenantRegistrationIsDisabledMessage_Detail"));
            }

            if (!_multiTenancyConfig.IsEnabled)
            {
                throw new UserFriendlyException(L("MultiTenancyIsNotEnabled"));
            }
        }

        private bool IsSelfRegistrationEnabled()
        {
            return SettingManager.GetSettingValueForApplication<bool>(
                AppSettings.TenantManagement.AllowSelfRegistration);
        }

        private bool UseCaptchaOnRegistration()
        {
            return SettingManager.GetSettingValueForApplication<bool>(AppSettings.TenantManagement
                .UseCaptchaOnRegistration);
        }

        private async Task CheckEditionSubscriptionAsync(int editionId, SubscriptionStartType subscriptionStartType)
        {
            var edition = await _editionManager.GetByIdAsync(editionId) as SubscribableEdition;

            CheckSubscriptionStart(edition, subscriptionStartType);
        }

        private static void CheckSubscriptionStart(SubscribableEdition edition,
            SubscriptionStartType subscriptionStartType)
        {
            switch (subscriptionStartType)
            {
                case SubscriptionStartType.Free:
                    if (!edition.IsFree)
                    {
                        throw new Exception("This is not a free edition !");
                    }
                    break;
                case SubscriptionStartType.Trial:
                    if (!edition.HasTrial())
                    {
                        throw new Exception("Trial is not available for this edition !");
                    }
                    break;
                case SubscriptionStartType.Paid:
                    if (edition.IsFree)
                    {
                        throw new Exception("This is a free edition and cannot be subscribed as paid !");
                    }
                    break;
            }
        }

        private async Task CheckTooManyFreeAccount(string emailAddress)
        {
            emailAddress = emailAddress.Trim().ToLower();

            try
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
                {
                    var freeEditionId = await _editionAppService.GetFreeEditionId();

                    //var users = _userRepository.GetAll().Where(e => e.IsDeleted == false && string.IsNullOrEmpty(e.EmailAddress) == false && e.NormalizedEmailAddress == emailAddress.ToUpper());

                    var counts = await (from o in _tenantRepository.GetAll().Where(e => e.NormalizedRegistrantEmail == emailAddress)
                                        group o by 1 into g
                                        select new
                                        {
                                            Paid = g.Count(e => e.EditionId != freeEditionId),
                                            Free = g.Count(e => e.EditionId == freeEditionId),
                                        }).FirstOrDefaultAsync();
                    if (counts != null && counts.Free > counts.Paid + 3 && counts.Free > counts.Paid * 2)
                        throw new UserFriendlyException(L("EmailCannotRegisterTooManyFreeAccount"));
                }
            }
            catch (Exception)
            {
            }
        }
    }
}