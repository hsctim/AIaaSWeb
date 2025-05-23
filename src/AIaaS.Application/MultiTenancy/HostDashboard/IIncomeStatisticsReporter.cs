﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIaaS.MultiTenancy.HostDashboard.Dto;

namespace AIaaS.MultiTenancy.HostDashboard
{
    public interface IIncomeStatisticsService
    {
        Task<List<IncomeStastistic>> GetIncomeStatisticsData(DateTime startDate, DateTime endDate,
            ChartDateInterval dateInterval);
    }
}