// Decompiled with JetBrains decompiler
// Type: ApiProtectorDotNet.ApiProtectorHandler
// Assembly: ApiProtector.NETCore, Version=2.2.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A202D1CE-F4BD-4598-A5C9-2128238B479B
// Assembly location: C:\AIAAS\apiprotector-dotnet-1.2.0.0\1.2.0.0\lib\ApiProtector.NETCore.dll

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiProtectorDotNet
{
    public class ApiProtectorHandler
    {
        private const uint CACHE_SIZE = 100000;
        private readonly Dictionary<string, ApiProtectionInfo> _cache = new Dictionary<string, ApiProtectionInfo>();
        private ApiProtectionRule _rule;

        private ApiProtectionInfo ApiProtectionInfo
        {
            get
            {
                ApiProtectionInfo apiProtectionInfo;
                if (this._cache.ContainsKey(this.Tag))
                {
                    apiProtectionInfo = this._cache[this.Tag];
                    if (apiProtectionInfo.ExpiresAt < DateTime.UtcNow)
                        apiProtectionInfo = new ApiProtectionInfo()
                        {
                            ExpiresAt = DateTime.UtcNow.AddSeconds((double)this.Rule.TimeWindowSeconds),
                            Penalized = false,
                            RequestCount = 0U
                        };
                }
                else
                    apiProtectionInfo = new ApiProtectionInfo()
                    {
                        ExpiresAt = DateTime.UtcNow.AddSeconds((double)this.Rule.TimeWindowSeconds),
                        Penalized = false,
                        RequestCount = 0U
                    };
                this._cache[this.Tag] = apiProtectionInfo;
                return apiProtectionInfo;
            }
        }

        private ulong Remaining => this.ApiProtectionInfo.RequestCount <= this.Rule.Limit ? (ulong)(this.Rule.Limit - this.ApiProtectionInfo.RequestCount) : 0UL;

        private bool RequestIsExcluded => !string.IsNullOrEmpty(this.Identity) && ApiProtectorConfig.ExclusionsByIdentity.Contains<string>(this.Identity, (IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase) || !string.IsNullOrEmpty(this.IpAddress) && ApiProtectorConfig.ExclusionsByIpAddress.Contains<string>(this.IpAddress, (IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase);

        private DateTime ResetDateTime => this.ApiProtectionInfo.ExpiresAt;

        private string Tag
        {
            get
            {
                switch (this.Rule.Type)
                {
                    case ApiProtectionType.Default:
                        return string.IsNullOrEmpty(this.Method) ? string.Empty : string.Format("method/{0}", (object)this.Method);
                    case ApiProtectionType.ByIdentity:
                        return string.IsNullOrEmpty(this.Identity) ? string.Empty : string.Format("id/{0}", (object)this.Identity.ToLowerInvariant());
                    case ApiProtectionType.ByIpAddress:
                        return string.IsNullOrEmpty(this.IpAddress) ? string.Empty : string.Format("ip/{0}", (object)this.IpAddress);
                    case ApiProtectionType.ByRole:
                        return string.IsNullOrEmpty(this.Role) || !this.Roles.Contains<string>(this.Role, (IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase) ? string.Empty : string.Format("role/{0}", (object)this.Role.ToLowerInvariant());
                    default:
                        return string.Empty;
                }
            }
        }

        internal string Identity { get; set; }

        internal string IpAddress { get; set; }

        internal string Method { get; set; }

        internal string Role { get; }

        internal List<string> Roles { get; } = new List<string>();

        internal uint CacheCount => (uint)this._cache.Count;

        internal bool RequestAllowed
        {
            get
            {
                if (string.IsNullOrEmpty(this.Tag) || this.RequestIsExcluded)
                    return true;
                int num = this.Remaining > 0UL ? 1 : 0;
                if (num != 0)
                    return num != 0;
                this.OnLimitReached();
                return num != 0;
            }
        }

        public ApiProtectionRule Rule
        {
            get => this._rule;
            set
            {
                if (value.Limit < 1U)
                    value.Limit = 1U;
                if (value.TimeWindowSeconds < 1U)
                    value.TimeWindowSeconds = 1U;
                this._rule = value;
            }
        }

        public ApiProtectorHandler(ApiProtectionRule rule, string role = null)
        {
            if (rule.Type == ApiProtectionType.ByRole)
            {
                if (string.IsNullOrEmpty(role))
                    throw new ArgumentNullException(nameof(role));
            }
            else if (!string.IsNullOrEmpty(role))
                throw new ArgumentException((string)null, nameof(role));
            this.Role = role;
            this.Rule = rule;
        }

        private void Penalize()
        {
            if (!this._cache.ContainsKey(this.Tag))
                return;
            ApiProtectionInfo apiProtectionInfo = this._cache[this.Tag];
            if (apiProtectionInfo.Penalized)
                return;
            if (this.Rule.PenaltySeconds > 0U)
                apiProtectionInfo.ExpiresAt = apiProtectionInfo.ExpiresAt.AddSeconds((double)this.Rule.PenaltySeconds);
            apiProtectionInfo.Penalized = true;
            this._cache[this.Tag] = apiProtectionInfo;
        }

        internal void IncrementRequestCount()
        {
            if (this._cache.Count > 100000)
                this._cache.Clear();
            if (this._cache.ContainsKey(this.Tag))
            {
                ApiProtectionInfo apiProtectionInfo = this._cache[this.Tag];
                if (apiProtectionInfo.ExpiresAt > DateTime.UtcNow)
                {
                    ++apiProtectionInfo.RequestCount;
                    this._cache[this.Tag] = apiProtectionInfo;
                }
                else
                    this._cache[this.Tag] = new ApiProtectionInfo()
                    {
                        ExpiresAt = DateTime.UtcNow.AddSeconds((double)this.Rule.TimeWindowSeconds),
                        Penalized = false,
                        RequestCount = 1U
                    };
            }
            else
                this._cache[this.Tag] = new ApiProtectionInfo()
                {
                    ExpiresAt = DateTime.UtcNow.AddSeconds((double)this.Rule.TimeWindowSeconds),
                    Penalized = false,
                    RequestCount = 1U
                };
        }

        internal void SetHeaders(IHeaderDictionary headers)
        {
            if (headers == null || string.IsNullOrEmpty(ApiProtectorConfig.HeaderName))
                return;
            bool penalized = this.ApiProtectionInfo.Penalized;
            string str = string.Format("{0}[{1}:{2}:{3}:{4}:{5}]", penalized ? (object)"!" : (object)string.Empty, (object)this.Remaining, (object)this.Rule.Limit, (object)this.Rule.TimeWindowSeconds, (object)this.ResetDateTime.Ticks, (object)(uint)(penalized ? (int)this.Rule.PenaltySeconds : 0));
            ((IDictionary<string, StringValues>)headers).Add(ApiProtectorConfig.HeaderName, str);
        }

        private void OnLimitReached()
        {
            if (!this.ApiProtectionInfo.Penalized)
                ApiProtectorNotifier.OnLimitReached((object)this, new ApiProtectorEventArgs()
                {
                    DateTime = DateTime.UtcNow,
                    MethodName = this.Method,
                    Rule = this.Rule,
                    Tag = this.Tag
                });
            this.Penalize();
        }
    }
}
