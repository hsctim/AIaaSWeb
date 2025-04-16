using System.Collections.Generic;

namespace AIaaS.Tenants.Dashboard.Dto
{
    public class GetVisitorStatisticsDataOutput
    {
        public List<StastisticBase> VisitorStatistics { get; set; }

        public GetVisitorStatisticsDataOutput(List<StastisticBase> visitorStatistics)
        {
            VisitorStatistics = visitorStatistics;
        }
    }
}