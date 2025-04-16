using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using AIaaS.Authorization.Accounts.Dto;

namespace AIaaS.Authorization.Accounts
{
    public interface IAccountAppService : IApplicationService
    {
        Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

        Task<int?> ResolveTenantId(ResolveTenantIdInput input);

        Task<RegisterOutput> Register(RegisterInput input);

        Task SendPasswordResetCode(SendPasswordResetCodeInput input);

        Task<ResetPasswordOutput> ResetPassword(ResetPasswordInput input);

        Task<ResetPasswordOutput> ResetPasswordForAllTenants(ResetPasswordInput input);

        Task SendEmailActivationLink(SendEmailActivationLinkInput input);

        Task ActivateEmail(ActivateEmailInput input);
        
        Task<ImpersonateOutput> ImpersonateUser(ImpersonateUserInput input);
        
        Task<ImpersonateOutput> ImpersonateTenant(ImpersonateTenantInput input);

        Task<ImpersonateOutput> DelegatedImpersonate(DelegatedImpersonateInput input);
        
        Task<ImpersonateOutput> BackToImpersonator();

        Task<SwitchToLinkedAccountOutput> SwitchToLinkedAccount(SwitchToLinkedAccountInput input);

        Task<List<TenantUser>> GetAllUsersByEmailPassword(GetAllUsersByEmailPasswordDto input);

    }
}
