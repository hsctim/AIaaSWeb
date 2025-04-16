using System.Threading.Tasks;
using AIaaS.Sessions.Dto;

namespace AIaaS.Web.Session
{
    public interface IPerRequestSessionCache
    {
        Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformationsAsync();
    }
}
