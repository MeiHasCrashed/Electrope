// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Electrope.IoC;

public static class DalamudServiceExtensions
{
    public static IServiceCollection AddDalamudService<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<T>(provider =>
        {
            var pluginInterface = provider.GetRequiredService<IDalamudPluginInterface>();
            var service = DalamudServiceHelper<T>.GetService(pluginInterface);
            if (service is IDisposable)
            {
                throw new InvalidServiceException(
                    "Dalamud services must not implement IDisposable, to prevent double disposal issues.");
            }

            return service;
        });
        return services;
    }
}
