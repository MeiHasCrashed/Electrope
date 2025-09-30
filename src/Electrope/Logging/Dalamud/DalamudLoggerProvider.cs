// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Concurrent;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;

namespace Electrope.Logging.Dalamud;

public sealed class DalamudLoggerProvider(IPluginLog logger) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, new DalamudLogger(categoryName, logger));

    public void Dispose()
    {
        _loggers.Clear();
    }
}
