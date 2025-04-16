using Abp.Dependency;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIaaS.Web.Security
{
    public class AntiDDoS : ISingletonDependency
    {
        public ConcurrentDictionary<string, ConcurrentQueue<long>> _dic = new ConcurrentDictionary<string, ConcurrentQueue<long>>();

        private static object __lockObj = new object();


        public bool isLimited(string ip_id, string functionName, int seconds, int limits)
        {
            try
            {
                lock (__lockObj)
                {
                    if (_dic.Count > 10000)
                        _dic = new ConcurrentDictionary<string, ConcurrentQueue<long>>();

                    ConcurrentQueue<long> queueTickCount;
                    _dic.TryGetValue(ip_id + functionName, out queueTickCount);
                    queueTickCount ??= new ConcurrentQueue<long>();

                    var tick64Now = Environment.TickCount64;

                    while (queueTickCount.Count >= limits)
                    {
                        long tickCount64;
                        if (queueTickCount.TryPeek(out tickCount64))
                        {
                            if (tickCount64 + seconds * 1000 < tick64Now)
                                queueTickCount.TryDequeue(out tickCount64);
                            else
                                break;
                        }
                        else
                            break;
                    }

                    if (queueTickCount.Count >= limits)
                        return true;

                    queueTickCount.Enqueue(tick64Now);

                    _dic[ip_id + functionName] = queueTickCount;

                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
