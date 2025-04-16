using System.Collections.Generic;
using AIaaS.Authorization.Users.Dto;
using AIaaS.Dto;

namespace AIaaS.Authorization.Users.Exporting
{
    public interface IUserListExcelExporter
    {
        FileDto ExportToFile(List<UserListDto> userListDtos);
    }
}