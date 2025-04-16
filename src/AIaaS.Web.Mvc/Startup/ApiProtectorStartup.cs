using ApiProtectorDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProtectorDotNet
{
    internal static class ApiProtectorStartup
    {

        internal static void Configure()
        {
            //ApiProtectorConfig.ExclusionsByIdentity.Add("myprettyuser");
            //ApiProtectorConfig.ExclusionsByIpAddress.Add("127.0.0.1");
            //ApiProtectorConfig.ExclusionsByIpAddress.Add("::1");
            //ApiProtectorConfig.HeaderName = "X-API-Protector";
            ApiProtectorConfig.HeaderName = "";

            //You can subscribe to the LimitReached event ...
            ApiProtectorNotifier.LimitReached += ApiProtectorNotifier_LimitReached;
        }

        private static void ApiProtectorNotifier_LimitReached(object sender, ApiProtectorEventArgs e)
        {
            //You can use this event to log/audit, each limit reached in you api/webapp.
            //Here you can take actions against DoS/DDoS attacks, send notifications, and more.
            //The event carries all the info that you need.
            Debug.WriteLine(e);
            //Logger.Log(LogSeverity.Error, $"An error occured while deleting audit logs on host database", e);

            //You can get here the ApiProtectorHandler associated to the event ...
            var handler = sender as ApiProtectorHandler;
            if (handler == null) { return; }

            //With it, you can set crazy dynamic rules ... LIKE THIS:
            //Uncomment the next block to apply a sample dynamic rule:
            /* 
            if (e.Rule.PenaltySeconds > 0) { //if the rule that has triggered the LimitReached event has any penalty set ...
                if (e.Rule.PenaltySeconds < 60) { //and, if that penalty is currently less than 60 seconds ...
                    //then, we will increase that penalty in one seconds, for the method that was generated the event ...
                    //every time that the limit is reached, until the penalty has reached 60 seconds.
                    handler.Rule = e.Rule.IncreasePenaltySeconds(1);
                }
            }
            */
        }
    }
}
