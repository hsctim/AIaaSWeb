using Abp.AutoMapper;
using AIaaS.MultiTenancy.Dto;

namespace AIaaS.Web.Models.TenantRegistration
{
    [AutoMapFrom(typeof(EditionsSelectOutput))]
    public class EditionsSelectViewModel : EditionsSelectOutput
    {
    }
}
