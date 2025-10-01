// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using Dalamud.Plugin.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Electrope.Logging.Dalamud;

public static class DalamudLoggingExtensions
{
    [PublicAPI]
    public static ILoggingBuilder AddDalamudLogging(this ILoggingBuilder builder, IPluginLog? logger)
    {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, DalamudLoggerProvider>(provider =>
                logger is null ? new DalamudLoggerProvider(provider.GetRequiredService<IPluginLog>()) :
                    new DalamudLoggerProvider(logger)
            ));
        return builder;
    }
}
