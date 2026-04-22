namespace SackranyPawn.Traits.PawnEvents.Interfaces
{
    public interface IBusPublisher
    {
        public bool Publish(IEvent @event);
        public bool Publish<T>(IEvent @event, T data, bool includeNoDataChannel = false);
        public bool Publish<E>() where E : IEvent;
        public bool Publish<E,T>(T data, bool includeNoDataChannel = false) where E : IEvent;

        public bool Publish(int id);
        public bool Publish<T>(int id, T data, bool includeNoDataChannel = false);
    }
}