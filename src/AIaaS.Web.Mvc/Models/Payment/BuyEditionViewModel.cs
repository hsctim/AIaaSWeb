using System.Collections.Generic;
using AIaaS.Editions;
using AIaaS.Editions.Dto;
using AIaaS.MultiTenancy.Payments;
using AIaaS.MultiTenancy.Payments.Dto;

namespace AIaaS.Web.Models.Payment
{
    public class BuyEditionViewModel
    {
        public SubscriptionStartType? SubscriptionStartType { get; set; }

        public EditionSelectDto Edition { get; set; }

        public decimal? AdditionalPrice { get; set; }

        public EditionPaymentType EditionPaymentType { get; set; }

        public List<PaymentGatewayModel> PaymentGateways { get; set; }
    }
}
