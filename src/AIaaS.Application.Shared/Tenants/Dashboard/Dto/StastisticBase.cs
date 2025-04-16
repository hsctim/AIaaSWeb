using System;

namespace AIaaS.Tenants.Dashboard.Dto
{
    public class StastisticBase
    {
        public string Label { get; set; }
        public DateTime Date { get; set; }
        public decimal Value { get; set; }

        public StastisticBase()
        {


        }

        public StastisticBase(DateTime date)
        {
            Date = date;
        }

        public StastisticBase(DateTime date, decimal value)
        {
            Date = date;
            Value = value;
        }
    }
}