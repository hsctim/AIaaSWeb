// ReflectSoftware.Facebook
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Newtonsoft.Json;

namespace ReflectSoftware.Facebook.Messenger.Common.Enums
{
    public enum NotificationType
    {
        [JsonProperty("REGULAR")]
        Regular = 0,

        [JsonProperty("SILENT_PUSH")]
        SilentPush,

        [JsonProperty("NO_PUSH")]
        NoPush
    }
}
