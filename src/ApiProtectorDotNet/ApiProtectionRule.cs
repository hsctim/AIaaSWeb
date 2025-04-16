// Decompiled with JetBrains decompiler
// Type: ApiProtectorDotNet.ApiProtectionRule
// Assembly: ApiProtector.NETCore, Version=2.2.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A202D1CE-F4BD-4598-A5C9-2128238B479B
// Assembly location: C:\AIAAS\apiprotector-dotnet-1.2.0.0\1.2.0.0\lib\ApiProtector.NETCore.dll

namespace ApiProtectorDotNet
{
  public struct ApiProtectionRule
  {
    public uint Limit;
    public uint PenaltySeconds;
    public uint TimeWindowSeconds;
    public ApiProtectionType Type;

    public ApiProtectionRule DecreaseLimit(uint val = 1)
    {
      if (val == 0U)
        return this;
      if (this.Limit < val)
      {
        this.Limit = 1U;
        return this;
      }
      this.Limit -= val;
      return this;
    }

    public ApiProtectionRule IncreaseLimit(uint val = 1)
    {
      if (val == 0U)
        return this;
      if (this.Limit > (uint) int.MaxValue - val)
      {
        this.Limit = (uint) int.MaxValue;
        return this;
      }
      this.Limit += val;
      return this;
    }

    public ApiProtectionRule SetLimit(uint val)
    {
      if (val <= 1U)
      {
        this.Limit = 1U;
        return this;
      }
      this.Limit = val;
      return this;
    }

    public ApiProtectionRule DecreasePenaltySeconds(uint val = 1)
    {
      if (val == 0U)
        return this;
      if (this.PenaltySeconds < val)
      {
        this.PenaltySeconds = 1U;
        return this;
      }
      this.PenaltySeconds -= val;
      return this;
    }

    public ApiProtectionRule IncreasePenaltySeconds(uint val = 1)
    {
      if (val == 0U)
        return this;
      if (this.PenaltySeconds > (uint) int.MaxValue - val)
      {
        this.PenaltySeconds = (uint) int.MaxValue;
        return this;
      }
      this.PenaltySeconds += val;
      return this;
    }

    public ApiProtectionRule SetPenaltySeconds(uint val)
    {
      this.PenaltySeconds = val;
      return this;
    }

    public ApiProtectionRule DecreaseTimeWindowSeconds(uint val = 1)
    {
      if (val == 0U)
        return this;
      if (this.TimeWindowSeconds < val)
      {
        this.TimeWindowSeconds = 1U;
        return this;
      }
      this.TimeWindowSeconds -= val;
      return this;
    }

    public ApiProtectionRule IncreaseTimeWindowSeconds(uint val = 1)
    {
      if (val == 0U)
        return this;
      if (this.TimeWindowSeconds > (uint) int.MaxValue - val)
      {
        this.TimeWindowSeconds = (uint) int.MaxValue;
        return this;
      }
      this.TimeWindowSeconds += val;
      return this;
    }

    public ApiProtectionRule SetTimeWindowSeconds(uint val)
    {
      if (val <= 1U)
      {
        this.TimeWindowSeconds = 1U;
        return this;
      }
      this.TimeWindowSeconds = val;
      return this;
    }
  }
}
