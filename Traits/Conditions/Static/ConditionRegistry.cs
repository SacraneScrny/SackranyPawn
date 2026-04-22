using System;
using System.Collections.Generic;

using SackranyPawn.Cache;

namespace SackranyPawn.Traits.Conditions.Static
{
    public static class ConditionRegistry
    {
        static readonly Dictionary<Type, ICondition> _instances = new();
        static readonly object _lock = new();
        
        public static int Count => TypeRegistry<ICondition>.Count;
        public static int GetId<T>() where T : ICondition => TypeRegistry<ICondition>.Id<T>.Value;
        public static int GetId(Type type) => TypeRegistry<ICondition>.GetOrRegister(type);
        public static Type GetTypeById(int id) => TypeRegistry<ICondition>.GetTypeById(id);
        
        public static ICondition GetInstance<T>() where T : ICondition => ConditionId<T>.Instance;
        public static ICondition GetInstance(Type type) => GetOrCreateInstance(type);
        
        static ICondition GetOrCreateInstance(Type type)
        {
            lock (_lock)
            {
                if (_instances.TryGetValue(type, out var inst)) return inst;
                inst = (ICondition)Activator.CreateInstance(type);
                _instances[type] = inst;
                return inst;
            }
        }
        
        static class ConditionId<T> where T : ICondition
        {
            public static readonly ICondition Instance = GetOrCreateInstance(typeof(T));
        }
    }
}