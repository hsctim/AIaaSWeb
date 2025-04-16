using System.Collections.Generic;
using MvvmHelpers;
using AIaaS.Models.NavigationMenu;

namespace AIaaS.Services.Navigation
{
    public interface IMenuProvider
    {
        ObservableRangeCollection<NavigationMenuItem> GetAuthorizedMenuItems(Dictionary<string, string> grantedPermissions);
    }
}