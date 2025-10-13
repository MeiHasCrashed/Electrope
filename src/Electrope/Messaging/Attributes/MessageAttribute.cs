// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

namespace Electrope.Messaging.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class MessageAttribute : Attribute
{
    public ThreadAffinity ThreadAffinity { get; init; }

    public MessageAttribute(ThreadAffinity threadAffinity = ThreadAffinity.Default)
    {
        ThreadAffinity = threadAffinity;
    }

    internal static readonly MessageAttribute Default = new();
}
