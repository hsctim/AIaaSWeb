using AIaaS.Editions;
using AIaaS.Editions.Dto;
using AIaaS.MultiTenancy.Payments;
using AIaaS.Security;
using AIaaS.MultiTenancy.Payments.Dto;

namespace AIaaS.Web.Models.TenantRegistration
{
    public class TenantRegisterViewModel
    {
        public PasswordComplexitySetting PasswordComplexitySetting { get; set; }

        public int? EditionId { get; set; }

        public SubscriptionStartType? SubscriptionStartType { get; set; }

        public EditionSelectDto Edition { get; set; }

        public EditionPaymentType EditionPaymentType { get; set; }
    }
}
