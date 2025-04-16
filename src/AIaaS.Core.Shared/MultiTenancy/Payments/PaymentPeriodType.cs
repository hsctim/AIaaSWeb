namespace AIaaS.MultiTenancy.Payments
{
    //絕對不能更改
    public enum PaymentPeriodType
    {
        None = 0,
        Daily = 1,
        Weekly = 7,
        Monthly = 30,
        Annual = 365
    }
}