// ReflectSoftware.Facebook
// Copyright (c) 2020 ReflectSoftware Inc.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;

namespace ReflectSoftware.Facebook.Messenger.Common.Models
{
    [Obsolete("Location Quick Replies have be depreciated.")]
    public class LocationQuickReply : QuickReply
    {
        public LocationQuickReply() : base("location")
        {
        }
    }
}
