using System;
using System.Collections.Generic;
using System.Reflection;
using SackranyPawn.Entities.Modules;

namespace SackranyPawn.Cache
{
    internal static class LimbKeyReflectionCache
    {
        static readonly Dictionary<Type, HashKeyField[]> _cache = new();

        public static HashKeyField[] GetHashKeys(Type limbType)
        {
            if (_cache.TryGetValue(limbType, out var cached))
                return cached;

            var arr = Build(limbType);
            _cache[limbType] = arr;
            return arr;
        }

        static HashKeyField[] Build(Type type)
        {
            var stack = new Stack<Type>();
            var cur = type;
            while (cur != null && cur != typeof(object))
            {
                stack.Push(cur);
                cur = cur.BaseType;
            }

            var result = new List<HashKeyField>(8);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                var fields = t.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                Array.Sort(fields, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

                foreach (var field in fields)
                    if (field.GetCustomAttribute<HashKeyAttribute>() is { } attr)
                        result.Add(new HashKeyField(field, attr));
            }

            return result.ToArray();
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