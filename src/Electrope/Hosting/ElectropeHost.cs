// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Dalamud.Plugin.Services;
using Electrope.IoC;
using Electrope.Logging.Dalamud;
using Electrope.Logging.EventTracing;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Electrope.Hosting;

public static class ElectropeHost
{
    [PublicAPI]
    public static IHostApplicationBuilder CreateDefaultBuilder(ElectropeHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var environmentName = "Production";
        if (options.PluginInterface.IsDev)
        {
            environmentName = "Development";
        }
        else if (options.PluginInterface.IsTesting)
        {
            environmentName = "Staging";
        }

        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = options.PluginName ?? options.PluginInterface.InternalName,
            ContentRootPath = options.PluginInterface.ConfigDirectory.FullName,
            DisableDefaults = true,
            EnvironmentName = environmentName
        });

        var pluginLog = DalamudServiceHelper<IPluginLog>.GetService(options.PluginInterface);

        builder.Logging
            .ClearProviders()
            .SetMinimumLevel(LogLevel.Trace)
            .AddDalamudLogging(pluginLog);

        if (options.EventTracingGuid is not null)
        {
            var guid = Guid.Parse(options.EventTracingGuid);
            builder.Logging.AddEventTracingLogger(guid);
        }

        builder.Services
            .AddSingleton(options.PluginInterface)
            .AddSingleton(pluginLog);

        return builder;
    }
}
