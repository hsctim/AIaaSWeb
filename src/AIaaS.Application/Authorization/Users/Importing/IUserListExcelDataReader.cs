using System.Collections.Generic;
using AIaaS.Authorization.Users.Importing.Dto;
using Abp.Dependency;

namespace AIaaS.Authorization.Users.Importing
{
    public interface IUserListExcelDataReader: ITransientDependency
    {
        List<ImportUserDto> GetUsersFromExcel(byte[] fileBytes);
    }
}
