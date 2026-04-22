using System;
using System.Collections.Generic;

using SackranyPawn.Cache;

namespace SackranyPawn.Traits.PawnEvents
{
    public static class EventRegistry
    {
        public static int GetId<T>() where T : IEvent  => TypeRegistry<IEvent>.Id<T>.Value;
        public static int GetOrRegisterId(Type type)   => TypeRegistry<IEvent>.GetOrRegister(type);
        public static Type GetTypeById(int id)         => TypeRegistry<IEvent>.GetTypeById(id);

        static readonly Dictionary<Type, IEvent> _instances = new();
        static readonly object _lock = new();

        public static IEvent GetInstance<T>() where T : IEvent => EventId<T>.Instance;

        static class EventId<T> where T : IEvent
        {
            public static readonly IEvent Instance = GetOrCreateInstance(typeof(T));
        }
        static IEvent GetOrCreateInstance(Type type)
        {
            lock (_lock)
            {
                if (_instances.TryGetValue(type, out var inst))
                    return inst;

                if (type.IsAbstract) return null;

                inst = Activator.CreateInstance(type) as IEvent
                       ?? throw new ArgumentException($"No public parameterless ctor for {type.FullName}");

                _instances[type] = inst;
                return inst;
            }
        }
    }
}