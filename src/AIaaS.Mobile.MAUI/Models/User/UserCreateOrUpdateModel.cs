using Abp.AutoMapper;
using AIaaS.Authorization.Users.Dto;

namespace AIaaS.Mobile.MAUI.Models.User
{
    [AutoMapFrom(typeof(CreateOrUpdateUserInput))]
    public class UserCreateOrUpdateModel : CreateOrUpdateUserInput
    {

    }
}
