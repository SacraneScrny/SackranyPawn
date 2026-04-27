using System;
using System.Collections.Generic;

namespace SackranyPawn.Cache
{
    internal static class TypeRegistry<TBase> where TBase : class
    {
        static readonly Dictionary<Type, int> _typeToId = new();
        static readonly List<Type> _idToType = new();
        static int _nextId;

        public static int Count => _nextId;

        internal static void Reset()
        {
            _typeToId.Clear();
            _idToType.Clear();
            _nextId = 0;
        }

        public static int GetOrRegister(Type type)
        {
            if (!typeof(TBase).IsAssignableFrom(type)) return -1;
            if (_typeToId.TryGetValue(type, out var id)) return id;

            var newId = _nextId++;
            _typeToId[type] = newId;
            _idToType.Add(type);
            return newId;
        }

        public static int GetId(Type type)
        {
            return _typeToId.GetValueOrDefault(type, -1);
        }

        public static Type GetTypeById(int id)
        {
            return id >= 0 && id < _idToType.Count ? _idToType[id] : null;
        }

        public static class Id<T> where T : TBase
        {
            public static readonly int Value = GetOrRegister(typeof(T));
        }
    }
}