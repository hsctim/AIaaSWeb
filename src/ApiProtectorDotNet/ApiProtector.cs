// Decompiled with JetBrains decompiler
// Type: ApiProtectorDotNet.ApiProtector
// Assembly: ApiProtector.NETCore, Version=2.2.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A202D1CE-F4BD-4598-A5C9-2128238B479B
// Assembly location: C:\AIAAS\apiprotector-dotnet-1.2.0.0\1.2.0.0\lib\ApiProtector.NETCore.dll

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace ApiProtectorDotNet
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class ApiProtector : ActionFilterAttribute
    {
        private static readonly object _lock = new object();
        private ApiProtectorHandler _protectorHandler;

        public ApiProtector(
          ApiProtectionType Type,
          uint Limit,
          uint TimeWindowSeconds,
          uint PenaltySeconds = 0,
          string Role = null)
        {
            this._protectorHandler = new ApiProtectorHandler(new ApiProtectionRule()
            {
                Type = Type,
                Limit = Limit,
                TimeWindowSeconds = TimeWindowSeconds,
                PenaltySeconds = PenaltySeconds
            }, Role);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            lock (ApiProtector._lock)
            {
                if (!this.Configure((FilterContext)context))
                    return;
                this.Filter(context);
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            this._protectorHandler?.SetHeaders(((ActionContext)context)?.HttpContext?.Response?.Headers);
            base.OnActionExecuted(context);
        }

        private bool Configure(FilterContext context)
        {
            if (context == null)
                return false;
            this._protectorHandler.Method = string.Format("{0}.{1}", (object)((ControllerActionDescriptor)((ActionContext)context).ActionDescriptor)?.ControllerName, (object)((ControllerActionDescriptor)((ActionContext)context).ActionDescriptor)?.ActionName);
            if (string.IsNullOrEmpty(this._protectorHandler.Method))
                return false;
            this._protectorHandler.IpAddress = ((IHttpConnectionFeature)((ActionContext)context).HttpContext?.Features?.Get<IHttpConnectionFeature>())?.RemoteIpAddress?.ToString();
            IIdentity identity = ((ActionContext)context).HttpContext?.User?.Identity;
            this._protectorHandler.Identity = identity?.Name;
            this._protectorHandler.Roles.Clear();
            if (identity != null)
            {
                IEnumerable<Claim> claims = ((ClaimsIdentity)identity).Claims;
                IEnumerable<Claim> source = claims != null ? claims.Where<Claim>((Func<Claim, bool>)(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")) : (IEnumerable<Claim>)null;
                if (source != null && source.Any<Claim>())
                {
                    foreach (Claim claim in source)
                    {
                        if (!this._protectorHandler.Roles.Contains<string>(claim.Value, (IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase))
                            this._protectorHandler.Roles.Add(claim.Value);
                    }
                }
            }
            switch (this._protectorHandler.Rule.Type)
            {
                case ApiProtectionType.Default:
                    if (string.IsNullOrEmpty(this._protectorHandler.Method))
                        return false;
                    break;
                case ApiProtectionType.ByIdentity:
                    if (string.IsNullOrEmpty(this._protectorHandler.Identity))
                        return false;
                    break;
                case ApiProtectionType.ByIpAddress:
                    if (string.IsNullOrEmpty(this._protectorHandler.IpAddress))
                        return false;
                    break;
                case ApiProtectionType.ByRole:
                    if (!this._protectorHandler.Roles.Contains<string>(this._protectorHandler.Role, (IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase))
                        return false;
                    break;
            }
            return true;
        }

        private void Filter(ActionExecutingContext context)
        {
            if (!this.Configure((FilterContext)context))
                return;
            if (this._protectorHandler.RequestAllowed)
                this._protectorHandler.IncrementRequestCount();
            else
                context.Result = (IActionResult)new StatusCodeResult(429);
        }
    }
}
