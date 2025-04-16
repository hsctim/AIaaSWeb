using System;
using System.Collections.Generic;
using AIaaS.Editions.Dto;
using AIaaS.MultiTenancy.Payments;

namespace AIaaS.Web.Models.Payment
{
    public class UpgradeEditionViewModel
    {
        public EditionSelectDto Edition { get; set; }

        public PaymentPeriodType PaymentPeriodType { get; set; }

        public SubscriptionPaymentType SubscriptionPaymentType { get; set; }

        public decimal? AdditionalPrice { get; set; }

        public List<PaymentGatewayModel> PaymentGateways { get; set; }

        public DateTime? SubscriptionEndDateUtc { get; set; }
    }
}