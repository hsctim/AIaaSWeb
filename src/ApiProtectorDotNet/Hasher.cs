// Decompiled with JetBrains decompiler
// Type: ApiProtectorDotNet.Hasher
// Assembly: ApiProtector.NETCore, Version=2.2.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A202D1CE-F4BD-4598-A5C9-2128238B479B
// Assembly location: C:\AIAAS\apiprotector-dotnet-1.2.0.0\1.2.0.0\lib\ApiProtector.NETCore.dll

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ApiProtectorDotNet
{
  internal static class Hasher
  {
    internal static string GetMD5(this string input)
    {
      byte[] hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(input));
      StringBuilder stringBuilder = new StringBuilder();
      for (int index = 0; index < hash.Length; ++index)
        stringBuilder.Append(hash[index].ToString("X2"));
      return stringBuilder.ToString();
    }

    internal static string GetMD5(this IEnumerable<char> input) => Hasher.GetMD5(input.ToString());
  }
}
