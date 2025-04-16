//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using Abp.Application.Services.Dto;
//using AIaaS.MultiTenancy.Payments;

//namespace AIaaS.Editions.Dto
//{

//    public class GetNlpSelectEditionDto : NlpEditionDto
//    {
//        //public PaymentPeriodType PaymentPeriodType { get; set; }
//        public DateTime? SubscriptionEndDateUtc { get; set; }
//        public bool NewSubscriptionOrderable { get; set; }
//        public decimal NewSubscriptionMonthlyFee { get; set; }
//        public decimal NewSubscriptionAnnualFee { get; set; }
//        public bool OldSubscriptionUpgradable { get; set; }
//        public decimal SubscriptionUpgradationFee { get; set; }
//        public string ErrorMessage { get; set; }
//        public string UpgradationMessage { get; set; }

//        public int ProcessorAllocation { get; set; }
//    }
//}