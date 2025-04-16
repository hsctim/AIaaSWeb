using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AIaaS.EntityFrameworkCore;
using System;

namespace AIaaS.HealthChecks
{
    public class AIaaSDbContextHealthCheck : IHealthCheck
    {
        private readonly DatabaseCheckHelper _checkHelper;

        public AIaaSDbContextHealthCheck(DatabaseCheckHelper checkHelper)
        {
            _checkHelper = checkHelper;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            if (_checkHelper.Exist("db"))
            {
                return Task.FromResult(HealthCheckResult.Healthy("AIaaSDbContext connected to database."));
            }

            // Using Task.Run to execute garbage collection
            Task.Run(() => GC.Collect());

            return Task.FromResult(HealthCheckResult.Unhealthy("AIaaSDbContext could not connect to database"));
        }
    }
}
