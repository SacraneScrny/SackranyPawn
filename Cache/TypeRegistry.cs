using System;
using System.Collections.Generic;

namespace SackranyPawn.Cache
{
    public static class TypeRegistry<TBase> where TBase : class
    {
        static readonly Dictionary<Type, int> _typeToId = new();
        static readonly List<Type> _idToType = new();
        static int _nextId;
        static readonly object _lock = new();

        public static int Count { get { lock (_lock) return _nextId; } }

        public static int GetOrRegister(Type type)
        {
            lock (_lock)
            {
                if (_typeToId.TryGetValue(type, out var id))
                    return id;

                var newId = _nextId++;
                _typeToId[type] = newId;
                _idToType.Add(type);
                return newId;
            }
        }

        public static int GetId(Type type)
        {
            lock (_lock)
                return _typeToId.GetValueOrDefault(type, -1);
        }

        public static Type GetTypeById(int id)
        {
            lock (_lock)
                return id >= 0 && id < _idToType.Count ? _idToType[id] : null;
        }

        public static class Id<T> where T : TBase
        {
            public static readonly int Value = GetOrRegister(typeof(T));
        }
    }
}