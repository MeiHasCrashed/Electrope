// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using Dalamud.Plugin;
using JetBrains.Annotations;

namespace Electrope.Hosting;

[PublicAPI]
public class ElectropeHostOptions
{
    /// <summary>
    /// The name of your plugin. Will be used for the host ApplicationName.
    /// </summary>
    public string? PluginName { get; init; }

    /// <summary>
    /// Dalamud's plugin interface. Required to access Dalamud's IoC system.
    /// </summary>
    public required IDalamudPluginInterface PluginInterface { get; init; }

    /// <summary>
    /// If you want to use the EventTracing logging provider, set this to a custom GUID.
    /// You can use the provided GUID to capture ETW logs in string format (sent with EventWriteString).
    /// <remarks>This only works on OSPlatform.Windows, and will be a no-op everywhere else.</remarks>
    /// </summary>
    public string? EventTracingGuid { get; init; }

    /// <summary>
    /// Sets whether to use a formatting that is compatible with Mei's TraceSpy fork for EventTracing logs.
    /// This will format logs in a way that TraceSpy can display them in a better way.
    /// </summary>
    /// <remarks>This should only be enabled on the fork, otherwise you loose information. Defaults to false.</remarks>
    public bool UseTraceSpyFormatting { get; init; }
}
