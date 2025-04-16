using System.Collections.Generic;
using AIaaS.Editions.Dto;
using AIaaS.MultiTenancy.Payments;

namespace AIaaS.Web.Models.Payment
{
    public class ExtendEditionViewModel
    {
        public EditionSelectDto Edition { get; set; }

        public List<PaymentGatewayModel> PaymentGateways { get; set; }
    }
}