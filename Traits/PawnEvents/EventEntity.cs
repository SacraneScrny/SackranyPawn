namespace SackranyPawn.Traits.PawnEvents
{
    public interface IEvent
    {
        int Id { get; }
        IEvent Instance { get; }
    }

    public abstract class AEvent<TSelf> : IEvent
        where TSelf : AEvent<TSelf>
    {
        public int Id => EventRegistry.GetId<TSelf>();
        public IEvent Instance => EventRegistry.GetInstance<TSelf>();
        
        protected AEvent() { }
    }

    public readonly struct Event<T> where T : IEvent
    {
        public static readonly int Id = EventRegistry.GetId<T>();
        public static readonly IEvent Instance = EventRegistry.GetInstance<T>();
    }
}