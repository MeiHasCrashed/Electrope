// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

namespace Electrope.IoC;

public class InvalidServiceException(string msg) : Exception(msg);
