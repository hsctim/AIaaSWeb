using Abp.Auditing;
using AIaaS.Configuration.Dto;

namespace AIaaS.Configuration.Tenants.Dto
{
    public class TenantEmailSettingsEditDto : EmailSettingsEditDto
    {
        public bool UseHostDefaultEmailSettings { get; set; }
    }
}