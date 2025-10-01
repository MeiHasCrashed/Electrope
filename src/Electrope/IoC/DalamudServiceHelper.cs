// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace Electrope.IoC;

internal struct DalamudServiceHelper<T> where T : class
{
    [PluginService]
    private T? Service { get; set; }

    public static T GetService(IDalamudPluginInterface pluginInterface)
    {
        var helper = new DalamudServiceHelper<T>();
        pluginInterface.Inject(helper);
        return helper.Service ?? throw new InvalidOperationException($"Failed to get service of type {typeof(T).FullName}");
    }
}
