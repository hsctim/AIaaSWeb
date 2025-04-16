using System.Collections.Generic;
using AIaaS.Caching.Dto;

namespace AIaaS.Web.Areas.App.Models.Maintenance
{
    public class MaintenanceViewModel
    {
        public IReadOnlyList<CacheDto> Caches { get; set; }
    }
}