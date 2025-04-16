// Decompiled with JetBrains decompiler
// Type: ApiProtectorDotNet.ApiProtectorEventArgs
// Assembly: ApiProtector.NETCore, Version=2.2.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A202D1CE-F4BD-4598-A5C9-2128238B479B
// Assembly location: C:\AIAAS\apiprotector-dotnet-1.2.0.0\1.2.0.0\lib\ApiProtector.NETCore.dll

using System;

namespace ApiProtectorDotNet
{
  public class ApiProtectorEventArgs : EventArgs
  {
    public DateTime DateTime { get; set; }

    public string MethodName { get; set; }

    public ApiProtectionRule Rule { get; set; }

    public string Tag { get; set; }

    public override string ToString()
    {
      string str;
      if (this.Rule.PenaltySeconds <= 0U)
        str = string.Format("[{0}] [{1}] Reached {2} requests in {3} seconds limit on [{4}].", (object) this.DateTime, (object) this.Tag, (object) this.Rule.Limit, (object) this.Rule.TimeWindowSeconds, (object) this.MethodName);
      else
        str = string.Format("[{0}] [{1}] Reached {2} requests in {3} seconds limit on [{4}]. --> Applying penalty of {5} seconds.", (object) this.DateTime, (object) this.Tag, (object) this.Rule.Limit, (object) this.Rule.TimeWindowSeconds, (object) this.MethodName, (object) this.Rule.PenaltySeconds);
      return str;
    }
  }
}
