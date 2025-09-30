// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Runtime.InteropServices;

namespace Electrope.Logging.EventTracing;

public static partial class EventTracingInterop
{

    [LibraryImport("advapi32")]
    public static partial int EventRegister(ref Guid providerId, nint enableCallback, nint callbackContext,
        out EventTracingSafeHandle handle);

    [LibraryImport("advapi32")]
    internal static partial int EventUnregister(IntPtr handle);

    [LibraryImport("advapi32", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int
        EventWriteString(EventTracingSafeHandle handle, byte level, ulong keyword, string message);
}
