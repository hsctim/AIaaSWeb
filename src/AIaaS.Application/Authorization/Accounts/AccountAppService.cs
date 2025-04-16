using System;
using System.Threading.Tasks;
using System.Web;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Extensions;
using Abp.Runtime.Security;
using Abp.Runtime.Session;
using Abp.UI;
using Abp.Zero.Configuration;
using Microsoft.AspNetCore.Identity;
using AIaaS.Authorization.Accounts.Dto;
using AIaaS.Authorization.Impersonation;
using AIaaS.Authorization.Users;
using AIaaS.Configuration;
using AIaaS.Debugging;
using AIaaS.MultiTenancy;
using AIaaS.Security.Recaptcha;
using AIaaS.Url;
using AIaaS.Authorization.Delegation;
using Abp.Domain.Repositories;
using Abp.Timing;
using ApiProtectorDotNet;
using System.Collections.Generic;
using Abp.Domain.Uow;
using System.Linq;
using Abp.Application.Services;
using AIaaS.Notifications;
using System.Diagnostics;

namespace AIaaS.Authorization.Accounts
{
    public class AccountAppService : AIaaSAppServiceBase, IAccountAppService
    {
        public IAppUrlService AppUrlService { get; set; }

        public IRecaptchaValidator RecaptchaValidator { get; set; }

        private readonly IUserEmailer _userEmailer;
        private readonly UserRegistrationManager _userRegistrationManager;
        private readonly IImpersonationManager _impersonationManager;
        private readonly IUserLinkManager _userLinkManager;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IWebUrlService _webUrlService;
        private readonly IUserDelegationManager _userDelegationManager;
        private readonly IAppNotifier _appNotifier;

        public AccountAppService(
            IUserEmailer userEmailer,
            UserRegistrationManager userRegistrationManager,
            IImpersonationManager impersonationManager,
            IUserLinkManager userLinkManager,
            IPasswordHasher<User> passwordHasher,
            IWebUrlService webUrlService,
            IUserDelegationManager userDelegationManager,
            IAppNotifier appNotifier)

        {
            _userEmailer = userEmailer;
            _userRegistrationManager = userRegistrationManager;
            _impersonationManager = impersonationManager;
            _userLinkManager = userLinkManager;
            _passwordHasher = passwordHasher;
            _webUrlService = webUrlService;
            _appNotifier = appNotifier;

            AppUrlService = NullAppUrlService.Instance;
            RecaptchaValidator = NullRecaptchaValidator.Instance;
            _userDelegationManager = userDelegationManager;
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input)
        {
            var tenant = await TenantManager.FindByTenancyNameAsync(input.TenancyName);
            if (tenant == null)
            {
                return new IsTenantAvailableOutput(TenantAvailabilityState.NotFound);
            }

            if (!tenant.IsActive)
            {
                return new IsTenantAvailableOutput(TenantAvailabilityState.InActive);
            }

            return new IsTenantAvailableOutput(TenantAvailabilityState.Available, tenant.Id, _webUrlService.GetServerRootAddress(input.TenancyName));
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public Task<int?> ResolveTenantId(ResolveTenantIdInput input)
        {
            if (string.IsNullOrEmpty(input.c))
            {
                return Task.FromResult(AbpSession.TenantId);
            }

            var parameters = SimpleStringCipher.Instance.Decrypt(input.c);
            var query = HttpUtility.ParseQueryString(parameters);

            if (query["tenantId"] == null)
            {
                return Task.FromResult<int?>(null);
            }

            var tenantId = Convert.ToInt32(query["tenantId"]) as int?;
            return Task.FromResult(tenantId);
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<RegisterOutput> Register(RegisterInput input)
        {
            if (UseCaptchaOnRegistration())
            {
                await RecaptchaValidator.ValidateAsync(input.CaptchaResponse);
            }

            var user = await _userRegistrationManager.RegisterAsync(
                input.Name,
                input.Surname,
                input.EmailAddress,
                input.UserName,
                input.Password,
                false,
                AppUrlService.CreateEmailActivationUrlFormat(AbpSession.TenantId)
            );

            var isEmailConfirmationRequiredForLogin = await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement.IsEmailConfirmationRequiredForLogin);

            return new RegisterOutput
            {
                CanLogin = user.IsActive && (user.IsEmailConfirmed || !isEmailConfirmationRequiredForLogin)
            };
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task SendPasswordResetCode(SendPasswordResetCodeInput input)
        {
            var user = await UserManager.FindByEmailAsync(input.EmailAddress);
            if (user == null)
            {
                await Task.Delay(new Random(DateTime.Now.Millisecond).Next(2000, 5000)); // delay a random duration between 2 and 5 seconds to simulate sending an email
                return;
            }

            user.SetNewPasswordResetCode();
            await _userEmailer.SendPasswordResetLinkAsync(
                user,
                AppUrlService.CreatePasswordResetUrlFormat(AbpSession.TenantId)
            );
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ResetPasswordOutput> ResetPassword(ResetPasswordInput input)
        {
            if (input.ExpireDate < Clock.Now)
            {
                throw new UserFriendlyException(L("PasswordResetLinkExpired"));
            }
            
            var user = await UserManager.GetUserByIdAsync(input.UserId);
            if (user == null || user.PasswordResetCode.IsNullOrEmpty() || user.PasswordResetCode != input.ResetCode)
            {
                throw new UserFriendlyException(L("InvalidPasswordResetCode"), L("InvalidPasswordResetCode_Detail"));
            }

            await UserManager.InitializeOptionsAsync(AbpSession.TenantId);
            CheckErrors(await UserManager.ChangePasswordAsync(user, input.Password));
            user.PasswordResetCode = null;
            user.IsEmailConfirmed = true;
            user.ShouldChangePasswordOnNextLogin = false;

            await UserManager.UpdateAsync(user);

            await _appNotifier.MyPasswordChanged(user, L("MyPasswordHasChanged"));

            return new ResetPasswordOutput
            {
                CanLogin = user.IsActive,
                UserName = user.UserName
            };
        }



        [RemoteService(false)]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ResetPasswordOutput> ResetPasswordForAllTenants(ResetPasswordInput input)
        {
            Debug.Assert(false, "This is very very very dangerous for security!");
            if (input.ExpireDate < Clock.Now)
            {
                throw new UserFriendlyException(L("PasswordResetLinkExpired"));
            }

            var user = await UserManager.GetUserByIdAsync(input.UserId);
            if (user == null || user.PasswordResetCode.IsNullOrEmpty() || user.PasswordResetCode != input.ResetCode)
            {
                throw new UserFriendlyException(L("InvalidPasswordResetCode"), L("InvalidPasswordResetCode_Detail"));
            }

            var users = GetUsersByChecking(user.EmailAddress);
            foreach (var u in users)
            {
                using (CurrentUnitOfWork.SetTenantId(u.TenantId))
                {
                    await UserManager.InitializeOptionsAsync(u.TenantId);
                    CheckErrors(await UserManager.ChangePasswordAsync(u, input.Password));
                    //u.PasswordResetCode = null;
                    u.IsEmailConfirmed = true;
                    u.ShouldChangePasswordOnNextLogin = false;

                    await _appNotifier.MyPasswordChanged(u, L("MyPasswordHasChanged"));
                    await UserManager.UpdateAsync(u);
                }
            }

            return new ResetPasswordOutput
            {
                CanLogin = user.IsActive,
                UserName = user.UserName
            };
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task SendEmailActivationLink(SendEmailActivationLinkInput input)
        {
            var user = await UserManager.FindByEmailAsync(input.EmailAddress);
            if (user == null)
            {
                return;
            }

            user.SetNewEmailConfirmationCode();
            await _userEmailer.SendEmailActivationLinkAsync(
                user,
                AppUrlService.CreateEmailActivationUrlFormat(AbpSession.TenantId)
            );
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task ActivateEmail(ActivateEmailInput input)
        {
            User user = null;
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                user = await UserManager.FindByIdAsync(input.UserId.ToString());
            }

            if (user == null)
                throw new UserFriendlyException(L("InvalidEmailConfirmationCode"), L("InvalidEmailConfirmationCode_Detail"));

            List<User> users = GetAllUsersByEmail(user.EmailAddress);
            users = (from u in users where u.TenantId.HasValue == true select u).ToList();

            var confirmedUserCount = (from u in users
                                      where u.EmailConfirmationCode == input.ConfirmationCode
                                      select u).Count();
            if (confirmedUserCount == 0)
            {
                throw new UserFriendlyException(L("InvalidEmailConfirmationCode"), L("InvalidEmailConfirmationCode_Detail"));
            }


            foreach (var u in users)
            {
                using (CurrentUnitOfWork.SetTenantId(u.TenantId))
                {
                    u.IsEmailConfirmed = true;
                    u.EmailConfirmationCode = null;
                    await UserManager.UpdateAsync(u);
                    //CurrentUnitOfWork.SetTenantId(u.TenantId);
                    //u.IsEmailConfirmed = true;
                    //u.EmailConfirmationCode = null;
                    //await UserManager.UpdateAsync(u);
                }
            }
        }

        [AbpAuthorize(AppPermissions.Pages_Administration_Users_Impersonation)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public virtual async Task<ImpersonateOutput> ImpersonateUser(ImpersonateUserInput input)
        {
            return new ImpersonateOutput
            {
                ImpersonationToken = await _impersonationManager.GetImpersonationToken(input.UserId, AbpSession.TenantId),
                TenancyName = await GetTenancyNameOrNullAsync(input.TenantId)
            };
        }

        [AbpAuthorize(AppPermissions.Pages_Administration_Users_Impersonation)]

        [AbpAuthorize(AppPermissions.Pages_Tenants_Impersonation)]
        public virtual async Task<ImpersonateOutput> ImpersonateTenant(ImpersonateTenantInput input)
        {
            return new ImpersonateOutput
            {
                ImpersonationToken = await _impersonationManager.GetImpersonationToken(input.UserId, input.TenantId),
                TenancyName = await GetTenancyNameOrNullAsync(input.TenantId)
            };
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public virtual async Task<ImpersonateOutput> DelegatedImpersonate(DelegatedImpersonateInput input)
        {
            var userDelegation = await _userDelegationManager.GetAsync(input.UserDelegationId);
            if (userDelegation.TargetUserId != AbpSession.GetUserId())
            {
                throw new UserFriendlyException("User delegation error.");
            }

            return new ImpersonateOutput
            {
                ImpersonationToken = await _impersonationManager.GetImpersonationToken(userDelegation.SourceUserId, userDelegation.TenantId),
                TenancyName = await GetTenancyNameOrNullAsync(userDelegation.TenantId)
            };
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public virtual async Task<ImpersonateOutput> BackToImpersonator()
        {
            return new ImpersonateOutput
            {
                ImpersonationToken = await _impersonationManager.GetBackToImpersonatorToken(),
                TenancyName = await GetTenancyNameOrNullAsync(AbpSession.ImpersonatorTenantId)
            };
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public virtual async Task<SwitchToLinkedAccountOutput> SwitchToLinkedAccount(SwitchToLinkedAccountInput input)
        {
            if (!await _userLinkManager.AreUsersLinked(AbpSession.ToUserIdentifier(), input.ToUserIdentifier()))
            {
                throw new Exception(L("This account is not linked to your account"));
            }

            return new SwitchToLinkedAccountOutput
            {
                SwitchAccountToken = await _userLinkManager.GetAccountSwitchToken(input.TargetUserId, input.TargetTenantId),
                TenancyName = await GetTenancyNameOrNullAsync(input.TargetTenantId)
            };
        }

        private bool UseCaptchaOnRegistration()
        {
            return SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.UseCaptchaOnRegistration);
        }

        private async Task<Tenant> GetActiveTenantAsync(int tenantId)
        {
            var tenant = await TenantManager.FindByIdAsync(tenantId);
            if (tenant == null)
            {
                throw new UserFriendlyException(L("UnknownTenantId{0}", tenantId));
            }

            if (!tenant.IsActive)
            {
                throw new UserFriendlyException(L("TenantIdIsNotActive{0}", tenantId));
            }

            return tenant;
        }

        private async Task<string> GetTenancyNameOrNullAsync(int? tenantId)
        {
            return tenantId.HasValue ? (await GetActiveTenantAsync(tenantId.Value)).TenancyName : null;
        }

        private List<User> GetUsersByChecking(string inputEmailAddress)
        {
            var users = GetAllUsersByEmail(inputEmailAddress).OrderBy(e => e.Id).ToList();

            //var user = await UserManager.FindByEmailAsync(inputEmailAddress);
            if (users.Count() == 0)
            {
                throw new UserFriendlyException(L("InvalidEmailAddress"));
            }

            return users;
        }

        public class TenantUserRaw
        {
            public Tenant tenant { set; get; }
            public User user { set; get; }
        }

        [RemoteService(false)]
        public async Task<List<TenantUser>> GetAllUsersByEmailPassword(GetAllUsersByEmailPasswordDto input)
        {
            List<TenantUserRaw> users = null;
            List<TenantUser> tenantUsers = new List<TenantUser>();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                users = (from u in UserManager.Users
                         join t in TenantManager.Tenants on u.TenantId equals t.Id into j
                         from jt in j.DefaultIfEmpty()
                         where
                         u.NormalizedEmailAddress == input.EmailAddress.ToUpper()
                         //u.EmailAddress.Equals(input.EmailAddress)
                         && (u.TenantId == null || jt != null)
                         select new TenantUserRaw()
                         {
                             tenant = jt,
                             user = u
                         }
                         ).ToList();
            }

            foreach (var user in users)
            {
                using (CurrentUnitOfWork.SetTenantId(user.user.TenantId))
                {
                    if (await UserManager.CheckPasswordAsync(user.user, input.Password))
                    {
                        tenantUsers.Add(new TenantUser()
                        {
                            TenantId = user.user.TenantId,
                            UserId = user.user.Id,
                            UserName = user.user.Name,
                            EmailAddress = user.user.EmailAddress,
                            TenantName = (user.tenant == null ? "" : user.tenant.Name),
                        });
                    }
                }
            }

            return tenantUsers;
        }

        [RemoteService(false)]
        public List<User> GetAllUsersByEmail(string inputEmailAddress)
        {
            List<User> users = null;
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                users = (from u in UserManager.Users
                         join t in TenantManager.Tenants on u.TenantId equals t.Id into j
                         from jt in j.DefaultIfEmpty()
                         where
                         u.NormalizedEmailAddress == inputEmailAddress.ToUpper() &&
                         (u.TenantId == null || jt != null)
                         select u
                         ).ToList();
            }

            return users;
        }
    }
}