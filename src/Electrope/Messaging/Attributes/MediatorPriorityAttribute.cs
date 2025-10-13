// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

namespace Electrope.Messaging.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MediatorPriorityAttribute : Attribute
{
    public MediatorPriority Priority { get; init; }

    public MediatorPriorityAttribute(MediatorPriority priority = MediatorPriority.Default)
    {
        Priority = priority;
    }

    internal static readonly MediatorPriorityAttribute Default = new();
}
