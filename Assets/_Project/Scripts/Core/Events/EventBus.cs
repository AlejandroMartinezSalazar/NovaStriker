using System;
using System.Collections.Generic;

namespace NovaStriker.Core.Events
{
    /// <summary>
    /// Implementación síncrona de IEventBus.
    /// Reglas:
    /// - Un handler que se suscribe durante una publicación empieza a recibir desde la siguiente.
    /// - Un handler que se desuscribe durante una publicación deja de recibir de inmediato.
    /// - Se puede publicar desde dentro de un handler (publicación anidada).
    /// </summary>
    public sealed class EventBus : IEventBus, IDisposable
    {
        private interface IChannel
        {
            int ActiveCount { get; }
            void Clear();
        }

        private sealed class Channel<T> : IChannel where T : struct, IGameEvent
        {
            private readonly List<Subscription<T>> _subscriptions = new List<Subscription<T>>();
            private int _publishDepth;
            private bool _needsCompaction;

            public int ActiveCount
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < _subscriptions.Count; i++)
                    {
                        if (_subscriptions[i].IsActive)
                        {
                            count++;
                        }
                    }
                    return count;
                }
            }

            public Subscription<T> Add(Action<T> handler)
            {
                var subscription = new Subscription<T>(this, handler);
                _subscriptions.Add(subscription);
                return subscription;
            }

            public void Remove(Subscription<T> subscription)
            {
                if (_publishDepth > 0)
                {
                    // No se modifica la lista mientras se recorre; se compacta al terminar.
                    _needsCompaction = true;
                    return;
                }

                _subscriptions.Remove(subscription);
            }

            public void Publish(T gameEvent, Action<Exception> onHandlerError)
            {
                // Se fija la cantidad al inicio: los suscritos durante esta publicación no la reciben.
                int count = _subscriptions.Count;
                _publishDepth++;
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        Subscription<T> subscription = _subscriptions[i];
                        if (!subscription.IsActive)
                        {
                            continue;
                        }

                        try
                        {
                            subscription.Handler(gameEvent);
                        }
                        catch (Exception exception)
                        {
                            onHandlerError(exception);
                        }
                    }
                }
                finally
                {
                    _publishDepth--;
                    if (_publishDepth == 0 && _needsCompaction)
                    {
                        _subscriptions.RemoveAll(s => !s.IsActive);
                        _needsCompaction = false;
                    }
                }
            }

            public void Clear()
            {
                for (int i = 0; i < _subscriptions.Count; i++)
                {
                    _subscriptions[i].Deactivate();
                }

                if (_publishDepth > 0)
                {
                    _needsCompaction = true;
                }
                else
                {
                    _subscriptions.Clear();
                }
            }
        }

        private sealed class Subscription<T> : IDisposable where T : struct, IGameEvent
        {
            private readonly Channel<T> _channel;

            public Subscription(Channel<T> channel, Action<T> handler)
            {
                _channel = channel;
                Handler = handler;
                IsActive = true;
            }

            public Action<T> Handler { get; }
            public bool IsActive { get; private set; }

            public void Deactivate() => IsActive = false;

            public void Dispose()
            {
                if (!IsActive)
                {
                    return;
                }

                IsActive = false;
                _channel.Remove(this);
            }
        }

        private readonly Dictionary<Type, IChannel> _channels = new Dictionary<Type, IChannel>();
        private readonly Action<Exception> _onHandlerError;
        private bool _isDisposed;

        /// <param name="onHandlerError">Qué hacer si un handler lanza una excepción. Por defecto: Debug.LogException.</param>
        public EventBus(Action<Exception> onHandlerError = null)
        {
            _onHandlerError = onHandlerError ?? UnityEngine.Debug.LogException;
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            ThrowIfDisposed();
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return GetOrCreateChannel<T>().Add(handler);
        }

        public void Publish<T>(T gameEvent) where T : struct, IGameEvent
        {
            ThrowIfDisposed();
            if (_channels.TryGetValue(typeof(T), out IChannel channel))
            {
                ((Channel<T>)channel).Publish(gameEvent, _onHandlerError);
            }
        }

        /// <summary>Número de suscripciones activas de un tipo. Útil para detectar suscripciones no liberadas.</summary>
        public int GetSubscriberCount<T>() where T : struct, IGameEvent
        {
            return _channels.TryGetValue(typeof(T), out IChannel channel) ? channel.ActiveCount : 0;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (IChannel channel in _channels.Values)
            {
                channel.Clear();
            }
            _channels.Clear();
        }

        private Channel<T> GetOrCreateChannel<T>() where T : struct, IGameEvent
        {
            if (_channels.TryGetValue(typeof(T), out IChannel existing))
            {
                return (Channel<T>)existing;
            }

            var channel = new Channel<T>();
            _channels.Add(typeof(T), channel);
            return channel;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(EventBus));
            }
        }
    }
}
