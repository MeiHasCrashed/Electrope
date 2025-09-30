// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Microsoft.Extensions.Logging;

namespace Electrope.Logging.EventTracing;

public class EventTracingLogger(string name, EventTracingProvider provider) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var message = $"[{logLevel.ToString()}] [{name}] {state}";
        if (exception is not null)
        {
            message += Environment.NewLine + exception;
        }
        provider.WriteString(logLevel, message);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null!;
}
