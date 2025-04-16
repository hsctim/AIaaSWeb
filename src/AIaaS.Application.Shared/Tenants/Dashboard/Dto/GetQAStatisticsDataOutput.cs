using System.Collections.Generic;

namespace AIaaS.Tenants.Dashboard.Dto
{
    public class GetQAStatisticsDataOutput
    {
        public List<StastisticBase> QAStatistics { get; set; }

        public GetQAStatisticsDataOutput(List<StastisticBase> qaStatistics)
        {
            QAStatistics = qaStatistics;
        }
    }
}