using Abp.AutoMapper;
using AIaaS.MultiTenancy;
using AIaaS.MultiTenancy.Dto;
using AIaaS.Web.Areas.App.Models.Common;

namespace AIaaS.Web.Areas.App.Models.Tenants
{
    [AutoMapFrom(typeof (GetTenantFeaturesEditOutput))]
    public class TenantFeaturesEditViewModel : GetTenantFeaturesEditOutput, IFeatureEditViewModel
    {
        public Tenant Tenant { get; set; }
    }
}