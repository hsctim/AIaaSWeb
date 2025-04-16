using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using AIaaS.MultiTenancy.Payments;

namespace AIaaS.Editions.Dto
{

    public enum SubscriptionType
    {
        New = 0,
        Upgrade = 1,
        Extented = 2,
    };

    public class InputSelectEditionDto
    {
        //public PaymentPeriodType PaymentPeriodType { get; set; }
        public int UserCount { get; set; }
        public int ChatbotCount { get; set; }
        public int QuestionCount { get; set; }
    }
}