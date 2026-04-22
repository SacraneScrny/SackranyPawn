using System;
using System.Collections.Generic;

using SackranyPawn.Traits.PawnEvents.Interfaces;

namespace SackranyPawn.Traits.PawnEvents
{
    public class PawnEvents : IBusListener, IBusPublisher
    {
        readonly Dictionary<(int id, Type type), Delegate> _events = new();

        static readonly Type NoDataType = typeof(NoData);
        sealed class NoData { }

        public void Subscribe(IEvent @event, Action callback) => SubscribeInternal((@event.Id, NoDataType), callback);
        public void Subscribe<T>(IEvent @event, Action<T> callback) => SubscribeInternal((@event.Id, typeof(T)), callback);
        public void Subscribe<E>(Action callback) where E : IEvent => SubscribeInternal((Event<E>.Id, NoDataType), callback);
        public void Subscribe<E, T>(Action<T> callback) where E : IEvent => SubscribeInternal((Event<E>.Id, typeof(T)), callback);
        
        public void Subscribe(int id, Action callback) => SubscribeInternal((id, NoDataType), callback);
        public void Subscribe<T>(int id, Action<T> callback) => SubscribeInternal((id, typeof(T)), callback);
        
        void SubscribeInternal((int, Type) key, Delegate d)
        {
            if (_events.TryGetValue(key, out var existing))
            {
                _events[key] = Delegate.Combine(existing, d);
            }
            else
            {
                _events[key] = d;
            }
        }

        public void Unsubscribe(IEvent @event, Action callback) => UnsubscribeInternal((@event.Id, NoDataType), callback);
        public void Unsubscribe<T>(IEvent @event, Action<T> callback) => UnsubscribeInternal((@event.Id, typeof(T)), callback);
        public void Unsubscribe<E>(Action callback) where E : IEvent => UnsubscribeInternal((Event<E>.Id, NoDataType), callback);
        public void Unsubscribe<E, T>(Action<T> callback) where E : IEvent => UnsubscribeInternal((Event<E>.Id, typeof(T)), callback);
        
        public bool Unsubscribe(int id, Action callback) => UnsubscribeInternal((id, NoDataType), callback);
        public bool Unsubscribe<T>(int id, Action<T> callback) => UnsubscribeInternal((id, typeof(T)), callback);
        
        bool UnsubscribeInternal((int, Type) key, Delegate d)
        {
            if (!_events.TryGetValue(key, out var existing)) return false;

            var current = Delegate.Remove(existing, d);
            if (current == existing) return false;

            if (current == null)
                _events.Remove(key);
            else
                _events[key] = current;

            return true;
        }

        public bool Publish(IEvent @event) => Publish(@event.Id);
        public bool Publish<T>(IEvent @event, T data, bool includeNoDataChannel = false) => Publish(@event.Id, data, includeNoDataChannel);
        public bool Publish<E>() where E : IEvent => Publish(Event<E>.Id);
        public bool Publish<E,T>(T data, bool includeNoDataChannel = false) where E : IEvent => Publish(Event<E>.Id, data, includeNoDataChannel);
        
        public bool Publish(int id)
        {
            if (!_events.TryGetValue((id, NoDataType), out var d)) return false;

            ((Action)d)();
            return true;
        }
        public bool Publish<T>(int id, T data, bool includeNoDataChannel = false)
        {
            bool invoked = false;

            if (_events.TryGetValue((id, typeof(T)), out var d))
            {
                ((Action<T>)d)(data);
                invoked = true;
            }

            if (includeNoDataChannel &&
                _events.TryGetValue((id, NoDataType), out var dl))
            {
                ((Action)dl)();
                invoked = true;
            }

            return invoked;
        }

        public void Reset()
        {
            _events.Clear();
        }
    }
}