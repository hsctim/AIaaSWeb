using System.Collections.Generic;
using AIaaS.Authorization.Delegation;
using AIaaS.Authorization.Users.Delegation.Dto;

namespace AIaaS.Web.Areas.App.Models.Layout
{
    public class ActiveUserDelegationsComboboxViewModel
    {
        public IUserDelegationConfiguration UserDelegationConfiguration { get; set; }

        public List<UserDelegationDto> UserDelegations { get; set; }

        public string CssClass { get; set; }
    }
}
