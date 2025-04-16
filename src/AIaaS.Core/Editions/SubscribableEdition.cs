using System;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Application.Editions;
using AIaaS.MultiTenancy.Payments;

namespace AIaaS.Editions
{
    /// <summary>
    /// Extends <see cref="Edition"/> to add subscription features.
    /// </summary>
    public class SubscribableEdition : Edition
    {
        /// <summary>
        /// The edition that will assigned after expire date
        /// </summary>
        public int? ExpiringEditionId { get; set; }

        //specified for the decimal property to decimal(18, 2)
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? DailyPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? WeeklyPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? MonthlyPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AnnualPrice { get; set; }

        public int? TrialDayCount { get; set; }

        /// <summary>
        /// The account will be taken an action (termination of tenant account) after the specified days when the subscription is expired.
        /// </summary>
        public int? WaitingDayAfterExpire { get; set; }

        [NotMapped]
        public bool IsFree => !DailyPrice.HasValue && !WeeklyPrice.HasValue && !MonthlyPrice.HasValue && !AnnualPrice.HasValue;

        public bool HasTrial()
        {
            if (IsFree)
            {
                return false;
            }

            return TrialDayCount.HasValue && TrialDayCount.Value > 0;
        }

        public decimal GetPaymentAmount(PaymentPeriodType? paymentPeriodType)
        {
            var amount = GetPaymentAmountOrNull(paymentPeriodType);
            if (!amount.HasValue)
            {
                throw new Exception("No price information found for " + DisplayName + " edition!");
            }

            return amount.Value;
        }

        public decimal? GetPaymentAmountOrNull(PaymentPeriodType? paymentPeriodType)
        {
            switch (paymentPeriodType)
            {
                case PaymentPeriodType.Daily:
                    return DailyPrice;
                case PaymentPeriodType.Weekly:
                    return WeeklyPrice;
                case PaymentPeriodType.Monthly:
                    return MonthlyPrice;
                case PaymentPeriodType.Annual:
                    return AnnualPrice;
                default:
                    return null;
            }
        }
    }
}