using System.Collections.Generic;
using AIaaS.Editions.Dto;

namespace AIaaS.Web.Areas.App.Models.Tenants
{
    public class TenantIndexViewModel
    {
        public List<SubscribableEditionComboboxItemDto> EditionItems { get; set; }
    }
}