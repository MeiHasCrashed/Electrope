// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Electrope.Messaging.Attributes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Electrope.Messaging;

public sealed class Mediator(ILogger<Mediator> logger, TimeSpan? timeout = null)
    : IMediator, IHostedService, IDisposable
{
    private readonly Dictionary<Type, List<MediatorSubscription>> _subscriptions = [];
    private readonly Dictionary<Type, ThreadAffinity> _messageThreadAffinities = [];
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly BlockingCollection<MessageContainer> _queue = [];
    private readonly CancellationTokenSource _cts = new();
    private Task? _mediatorQueueTask;
    private bool _shuttingDown;
    private bool _disposed;

    public void Subscribe<TMessage>(object subscriber, MessageHandler<TMessage> handler) where TMessage : IMessage
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Mediator));
        if (_shuttingDown)
        {
            logger.LogWarning("Mediator is shutting down, cannot subscribe {SubscriberType}.{FuncName} to {MessageType}.",
                subscriber.GetType(), handler.Method.Name, typeof(TMessage));
            return;
        }
        var messageType = typeof(TMessage);
        var messageAttribute = messageType.GetCustomAttribute<MessageAttribute>() ?? MessageAttribute.Default;
        var handlerPriorityAttribute = handler.Method.GetCustomAttribute<MediatorPriorityAttribute>();
        var classPriorityAttribute = subscriber.GetType().GetCustomAttribute<MediatorPriorityAttribute>() ?? MediatorPriorityAttribute.Default;
        var priority = handlerPriorityAttribute?.Priority ?? classPriorityAttribute.Priority;
        var subscription = new MediatorSubscription(new WeakReference(subscriber), msg => handler((TMessage)msg), priority);
        _lock.EnterWriteLock();
        try
        {
            if (!_subscriptions.TryGetValue(messageType, out var subscribers))
            {
                // We can fast exit here since there are no subscribers yet, so no duplicates are possible.
                subscribers = [subscription];
                _subscriptions[messageType] = subscribers;
                _messageThreadAffinities[messageType] = messageAttribute.ThreadAffinity;
                return;
            }

            if (subscribers.Exists(x => x.Handler == (Delegate)handler))
            {
                throw new InvalidOperationException($"Handler {handler.Method.Name} is already subscribed to {messageType} in {subscriber.GetType()}.");
            }
            subscribers.Add(subscription);

            // x 0, y 1 => 0 - 1 = -1 (x less than y)
            // x 1, y 0 => 1 - 0 = 1 (x greater than y)
            // x 0, y 0 => 0 - 0 = 0 (x equals y)
            subscribers.Sort((x, y) => (int)y.Priority - (int)x.Priority);
            _subscriptions[messageType] = subscribers;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Unsubscribe<TMessage>(object subscriber, MessageHandler<TMessage> handler) where TMessage : IMessage
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Mediator));
        var messageType = typeof(TMessage);
        _lock.EnterWriteLock();
        try
        {
            if (!_subscriptions.TryGetValue(messageType, out var subscribers))
            {
                return;
            }

            subscribers.RemoveAll(x => x.Subscriber == subscriber && x.Handler == (Delegate)handler);
            if (subscribers.Count == 0)
            {
                _subscriptions.Remove(messageType);
                _messageThreadAffinities.Remove(messageType);
            }
            else
            {
                // Probably not needed, but just in case.
                subscribers.Sort((x, y) => (int)y.Priority - (int)x.Priority);
                _subscriptions[messageType] = subscribers;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void UnsubscribeAll(object subscriber)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Mediator));
        _lock.EnterWriteLock();
        try
        {
            foreach (var (messageType, subscriptions) in _subscriptions.ToList())
            {
                subscriptions.RemoveAll(x => x.Subscriber == subscriber);
                if (subscriptions.Count == 0)
                {
                    _subscriptions.Remove(messageType);
                    _messageThreadAffinities.Remove(messageType);
                }
                else
                {
                    _subscriptions[messageType] = subscriptions;
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Publish<TMessage>(TMessage message) where TMessage : IMessage
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Mediator));
        if (_shuttingDown)
        {
            logger.LogWarning("Mediator is shutting down, cannot publish message of type {MessageType}.", typeof(TMessage));
            return;
        }
        var messageType = typeof(TMessage);
        var threadAffinity = _messageThreadAffinities.GetValueOrDefault(messageType, ThreadAffinity.Default);
        switch (threadAffinity)
        {
            case ThreadAffinity.SameThread:
                PublishInternal(message, messageType);
                break;
            case ThreadAffinity.Default:
                _queue.Add(new MessageContainer(messageType, message));
                break;
            default:
                throw new InvalidOperationException("Unknown thread affinity cached for message type " + messageType);
        }
    }

    private void PublishInternal(IMessage message, Type messageType)
    {
        MediatorSubscription[] subscribers;
        _lock.EnterReadLock();
        try
        {
            if (!_subscriptions.TryGetValue(messageType, out var rawSubscribers))
            {
                return;
            }

            subscribers = rawSubscribers.ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }

        var staleSubscribers = new List<MediatorSubscription>(subscribers.Length);
        foreach (var subscription in subscribers)
        {
            if (!subscription.Subscriber.IsAlive)
            {
                staleSubscribers.Add(subscription);
                continue;
            }

            try
            {
                subscription.Handler(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while handling message {MessageType} in subscriber {SubscriberType}.{FuncName}.",
                    messageType, subscription.Subscriber.Target?.GetType(), subscription.Handler.Method.Name);
            }
        }
        if (staleSubscribers.Count == 0) return;
        _ = Task.Run(() => RemoveStaleSubscribers(messageType, staleSubscribers));
    }

    private void RemoveStaleSubscribers(Type messageType, List<MediatorSubscription> staleSubscribers)
    {
        try
        {
            if (_disposed) return;
            _lock.EnterWriteLock();
            try
            {
                if (!_subscriptions.TryGetValue(messageType, out var subscribers))
                {
                    // Apparently got removed in between publish and now.
                    return;
                }

                subscribers.RemoveAll(staleSubscribers.Contains);
                if (subscribers.Count == 0)
                {
                    _subscriptions.Remove(messageType);
                    _messageThreadAffinities.Remove(messageType);
                }
                else
                {
                    _subscriptions[messageType] = subscribers;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while removing stale subscribers for message type {MessageType}.", messageType);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Electrope Mediator.");
        _mediatorQueueTask = Task.Factory.StartNew(async () =>
        {
            try
            {
                await ProcessQueue(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // We can ignore this, it's expected on shutdown if the queue is currently waiting.
            }
            catch (Exception ex)
            {
                logger.LogError("Unhandled exception in Mediator queue processing: {Exception}", ex);
            }
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        return Task.CompletedTask;
    }

    /// <summary>
    /// This is technically an async task, but it's running as a separate thread and uses a blocking collection.
    /// We just need await to handle potential timeout warnings.
    /// This will just run forever until the cancellation token is cancelled.
    /// </summary>
    /// <param name="cancellationToken"></param>
    private async Task ProcessQueue(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            var messageContainer = _queue.Take(cancellationToken);
            if (timeout == null)
            {
                try
                {
                    PublishInternal(messageContainer.Message, messageContainer.MessageType);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception while publishing message of type {MessageType}",
                        messageContainer.MessageType);
                }
                continue;
            }
            var publishTask = Task.Run(() =>
                {
                    try
                    {
                        PublishInternal(messageContainer.Message, messageContainer.MessageType);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unhandled exception while publishing message of type {MessageType}",
                            messageContainer.MessageType);
                    }
                },
                cancellationToken);
            var timeoutTask = Task.Delay(timeout.Value, CancellationToken.None);
            var completedTask = await Task.WhenAny(publishTask, timeoutTask);
            if (completedTask != timeoutTask) continue;
            logger.LogWarning("Publishing message of type {MessageType} took longer than the configured timeout of {Timeout}.",
                messageContainer.MessageType, timeout);
            await publishTask;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Electrope Mediator.");
        _shuttingDown = true;
        _queue.CompleteAdding();
        await _cts.CancelAsync();
        if (_mediatorQueueTask != null)
        {
            await _mediatorQueueTask;
        }
        _mediatorQueueTask = null;
        logger.LogInformation("Electrope Mediator stopped.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _subscriptions.Clear();
        _lock.Dispose();
        _queue.Dispose();
        _cts.Dispose();
        _disposed = true;
    }

    private record MessageContainer(Type MessageType, IMessage Message);
}
