// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Electrope.Logging.EventTracing;

public sealed class EventTracingLoggerProvider(EventTracingProvider provider) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, EventTracingLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, new EventTracingLogger(categoryName, provider));

    public void Dispose()
    {
        _loggers.Clear();
    }
}
