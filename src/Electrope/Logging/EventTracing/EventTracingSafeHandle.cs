// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Win32.SafeHandles;

namespace Electrope.Logging.EventTracing;

public class EventTracingSafeHandle() : SafeHandleZeroOrMinusOneIsInvalid(true)
{
    protected override bool ReleaseHandle()
    {
        return EventTracingInterop.EventUnregister(handle) == 0;
    }
}
