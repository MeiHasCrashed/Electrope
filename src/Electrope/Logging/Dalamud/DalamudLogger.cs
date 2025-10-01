// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Electrope.Logging.Dalamud;

public class DalamudLogger(string name, IPluginLog logger) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        logger.Write((LogEventLevel)logLevel, exception, $"[{name}] {state}");
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null!;
}
