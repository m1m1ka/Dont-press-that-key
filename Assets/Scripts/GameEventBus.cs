using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEventBus
{
    private static readonly Dictionary<Type, Delegate> SubscribersByEventType =
        new Dictionary<Type, Delegate>();

    public static IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        Type eventType = typeof(TEvent);
        if (SubscribersByEventType.TryGetValue(eventType, out Delegate existingHandlers))
        {
            SubscribersByEventType[eventType] = Delegate.Combine(existingHandlers, handler);
        }
        else
        {
            SubscribersByEventType.Add(eventType, handler);
        }

        return new EventSubscription<TEvent>(handler);
    }

    public static void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null)
        {
            return;
        }

        Type eventType = typeof(TEvent);
        if (!SubscribersByEventType.TryGetValue(eventType, out Delegate existingHandlers))
        {
            return;
        }

        Delegate remainingHandlers = Delegate.Remove(existingHandlers, handler);
        if (remainingHandlers == null)
        {
            SubscribersByEventType.Remove(eventType);
            return;
        }

        SubscribersByEventType[eventType] = remainingHandlers;
    }

    public static void Publish<TEvent>(TEvent gameEvent)
    {
        Type eventType = typeof(TEvent);
        if (!SubscribersByEventType.TryGetValue(eventType, out Delegate subscribers))
        {
            return;
        }

        Delegate[] invocationList = subscribers.GetInvocationList();
        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<TEvent>)invocationList[i]).Invoke(gameEvent);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    public static void Clear()
    {
        SubscribersByEventType.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        Clear();
    }

    private sealed class EventSubscription<TEvent> : IDisposable
    {
        private Action<TEvent> handler;

        public EventSubscription(Action<TEvent> handler)
        {
            this.handler = handler;
        }

        public void Dispose()
        {
            if (handler == null)
            {
                return;
            }

            GameEventBus.Unsubscribe(handler);
            handler = null;
        }
    }
}
