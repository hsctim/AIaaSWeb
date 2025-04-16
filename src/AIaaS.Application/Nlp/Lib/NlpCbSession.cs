using Abp.Dependency;
using Abp.Runtime.Caching;
using Abp.Runtime.Session;
using System;
using System.Linq;
using AIaaS.Helpers;

namespace AIaaS.Nlp
{
    //模擬Session
    public class NlpCbSession : ITransientDependency
    {
        const int ExpireTimeHours = 24;

        private readonly IAbpSession _abpSession;
        private readonly ICacheManager _cacheManager;
        //private static ConcurrentDictionary<string, NlpCbSessionItem> _session = new ConcurrentDictionary<string, NlpCbSessionItem>();
        //private static int usageCount = 0;

        public NlpCbSession(IAbpSession abpSession, ICacheManager cacheManager)
        {
            _abpSession = abpSession;
            _cacheManager = cacheManager;
        }


        public object this[string key]
        {
            get
            {
                key = _abpSession.UserId.ToString() + key;
                return _cacheManager.GetCache("AbpSession")?.GetOrDefault(key);
            }

            set
            {
                try
                {
                    key = _abpSession.UserId.ToString() + key;
                    _cacheManager.GetCache("AbpSession").Set(key, value, new TimeSpan(ExpireTimeHours, 0, 0));
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
