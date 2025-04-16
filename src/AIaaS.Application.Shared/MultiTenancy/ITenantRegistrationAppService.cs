using System.Threading.Tasks;
using Abp.Application.Services;
using AIaaS.Editions.Dto;
using AIaaS.MultiTenancy.Dto;

namespace AIaaS.MultiTenancy
{
    public interface ITenantRegistrationAppService : IApplicationService
    {
        Task<RegisterTenantOutput> RegisterTenant(RegisterTenantInput input);

        Task<EditionsSelectOutput> GetEditionsForSelect();

        Task<EditionSelectDto> GetEdition(int editionId);
    }
}