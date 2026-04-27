using System;
using System.Collections.Generic;
using System.Reflection;

using SackranyPawn.Entities.Modules;

namespace SackranyPawn.Cache
{
    public static class LimbKeyReflectionCache
    {
        static readonly Dictionary<Type, HashKeyField[]> _cache = new();

        public static HashKeyField[] GetHashKeys(Type limbType)
        {
            if (_cache.TryGetValue(limbType, out var cached))
                return cached;

            var fields = limbType.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            var result = new List<HashKeyField>();
            foreach (var field in fields)
                if (field.GetCustomAttribute<HashKeyAttribute>() is { } attr)
                    result.Add(new HashKeyField(field, attr));

            var arr = result.ToArray();
            _cache[limbType] = arr;
            return arr;
        }

        public readonly struct HashKeyField
        {
            public readonly FieldInfo Field;
            public readonly HashKeyAttribute Attr;
            public HashKeyField(FieldInfo field, HashKeyAttribute attr)
            {
                Field = field;
                Attr  = attr;
            }
        }
    }
}