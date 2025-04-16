// Decompiled with JetBrains decompiler
// Type: ApiProtectorDotNet.ApiProtectionInfo
// Assembly: ApiProtector.NETCore, Version=2.2.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A202D1CE-F4BD-4598-A5C9-2128238B479B
// Assembly location: C:\AIAAS\apiprotector-dotnet-1.2.0.0\1.2.0.0\lib\ApiProtector.NETCore.dll

using System;

namespace ApiProtectorDotNet
{
  public struct ApiProtectionInfo
  {
    public DateTime ExpiresAt;
    public bool Penalized;
    public uint RequestCount;
  }
}
