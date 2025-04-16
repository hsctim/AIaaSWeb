// Decompiled with JetBrains decompiler
// Type: ApiProtectorDotNet.ApiProtectorConfig
// Assembly: ApiProtector.NETCore, Version=2.2.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A202D1CE-F4BD-4598-A5C9-2128238B479B
// Assembly location: C:\AIAAS\apiprotector-dotnet-1.2.0.0\1.2.0.0\lib\ApiProtector.NETCore.dll

using System.Collections.Generic;

namespace ApiProtectorDotNet
{
  public static class ApiProtectorConfig
  {
    public static List<string> ExclusionsByIdentity { get; } = new List<string>();

    public static List<string> ExclusionsByIpAddress { get; } = new List<string>();

    public static string HeaderName { get; set; } = "X-API-Protector";
  }
}
