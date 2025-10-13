// Copyright (c) 2025 MeiHasCrashed
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Electrope.Messaging;

public interface IMediator
{
    public void Subscribe<TMessage>(object subscriber, MessageHandler<TMessage> handler) where TMessage : IMessage;

    public void Unsubscribe<TMessage>(object subscriber, MessageHandler<TMessage> handler) where TMessage : IMessage;

    public void UnsubscribeAll(object subscriber);

    public void Publish<TMessage>(TMessage message) where TMessage : IMessage;
}

public delegate void MessageHandler<in TMessage>(TMessage message) where TMessage : IMessage;
