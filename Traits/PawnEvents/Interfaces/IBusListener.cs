using System;

namespace SackranyPawn.Traits.PawnEvents.Interfaces
{
    public interface IBusListener
    {
        public void Subscribe(IEvent @event, Action callback);
        public void Subscribe<T>(IEvent @event, Action<T> callback);
        public void Subscribe<E>(Action callback) where E : IEvent;
        public void Subscribe<E, T>(Action<T> callback) where E : IEvent;
        
        public void Subscribe(int id, Action callback);
        public void Subscribe<T>(int id, Action<T> callback);

        public void Unsubscribe(IEvent @event, Action callback);
        public void Unsubscribe<T>(IEvent @event, Action<T> callback);
        public void Unsubscribe<E>(Action callback) where E : IEvent;
        public void Unsubscribe<E, T>(Action<T> callback) where E : IEvent;
        
        public bool Unsubscribe(int id, Action callback);
        public bool Unsubscribe<T>(int id, Action<T> callback);
    }
}