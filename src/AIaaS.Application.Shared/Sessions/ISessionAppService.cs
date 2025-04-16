using System.Threading.Tasks;
using Abp.Application.Services;
using AIaaS.Sessions.Dto;

namespace AIaaS.Sessions
{
    public interface ISessionAppService : IApplicationService
    {
        Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();

        Task<UpdateUserSignInTokenOutput> UpdateUserSignInToken();
    }
}
