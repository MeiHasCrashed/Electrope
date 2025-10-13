// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Electrope.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers the Mediator service with an optional queue timeout.
    /// </summary>
    /// <param name="services">The service collection to add the mediator to.</param>
    /// <param name="queueTimeout">The time a message can take to be processed before the mediator logs a warning.</param>
    /// <returns>The same service collection for chaining purposes.</returns>
    /// <remarks>If <see cref="queueTimeout"/> is null or not set the mediator will not attempt to check execution time,
    /// which removes the overhead of checking for execution time in parallel to executing the message in the hot path.</remarks>
    [PublicAPI]
    public static IServiceCollection AddMediator(this IServiceCollection services, TimeSpan? queueTimeout = null)
    {
        services.AddSingleton<Mediator>(provider =>
                new Mediator(provider.GetRequiredService<ILogger<Mediator>>(), queueTimeout));

        services.AddSingleton<IMediator>(x => x.GetRequiredService<Mediator>());
        services.AddHostedService(p => p.GetRequiredService<Mediator>());
        return services;
    }
}
