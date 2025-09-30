// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Electrope.Logging.EventTracing;

[SupportedOSPlatform("windows")]
public sealed class EventTracingProvider : IDisposable
{
    private EventTracingSafeHandle? _handle;
    private bool _disposed;

    public EventTracingProvider(Guid providerId)
    {
        var result = EventTracingInterop.EventRegister(ref providerId, nint.Zero, nint.Zero, out var handle);
        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to register event tracing provider. Error code: {result}");
        }
        if(handle.IsInvalid || handle.IsClosed)
        {
            throw new InvalidOperationException("Failed to register event tracing provider. Invalid handle.");
        }

        _handle = handle;
    }

    public void WriteString(LogLevel level, string message)
    {
        if (_disposed || _handle is null) return;
        if (_handle.IsInvalid || _handle.IsClosed) return;
        if (level == LogLevel.None) return;
        if (!LogLevelToEventLevel.TryGetValue(level, out var eventLevel))
        {
            eventLevel = 4; // Default to Information level
        }
        var result = EventTracingInterop.EventWriteString(_handle, eventLevel, 0, message);
        if (result != 0)
        {
            Debug.WriteLine("Failed to write event tracing string. Error code: {0}", result);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _handle?.Dispose();
        _handle = null;
        _disposed = true;
    }


    private static Dictionary<LogLevel, byte> LogLevelToEventLevel { get; } = new()
    {
        [LogLevel.Trace] = 5,
        [LogLevel.Debug] = 5,
        [LogLevel.Information] = 4,
        [LogLevel.Warning] = 3,
        [LogLevel.Error] = 2,
        [LogLevel.Critical] = 1,
    };
}
