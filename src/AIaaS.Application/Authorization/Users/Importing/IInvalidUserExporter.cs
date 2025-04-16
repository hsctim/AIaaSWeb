using System.Collections.Generic;
using AIaaS.Authorization.Users.Importing.Dto;
using AIaaS.Dto;

namespace AIaaS.Authorization.Users.Importing
{
    public interface IInvalidUserExporter
    {
        FileDto ExportToFile(List<ImportUserDto> userListDtos);
    }
}
