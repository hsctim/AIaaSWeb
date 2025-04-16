using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using AIaaS.Dto;

namespace AIaaS.Gdpr
{
    public interface IUserCollectedDataProvider
    {
        Task<List<FileDto>> GetFiles(UserIdentifier user);
    }
}
