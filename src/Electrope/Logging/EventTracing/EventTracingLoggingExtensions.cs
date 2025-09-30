// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Electrope.Logging.EventTracing;

public static class EventTracingLoggingExtensions
{
    public static ILoggingBuilder AddEventTracingLogging(this ILoggingBuilder builder, Guid providerId)
    {
        // EventTracing is only supported on Windows (ETW)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return builder;

        builder.Services.TryAddSingleton<EventTracingProvider>(provider => new EventTracingProvider(providerId));
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, EventTracingLoggerProvider>(provider =>
                new EventTracingLoggerProvider(provider.GetRequiredService<EventTracingProvider>())
            ));
        return builder;
    }
}
