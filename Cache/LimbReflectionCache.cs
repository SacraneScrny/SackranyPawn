using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SackranyPawn.Entities.Modules;

namespace SackranyPawn.Cache
{
    public static class LimbReflectionCache
    {
        public readonly struct LimbMetadata
        {
            public readonly DependencyField[] Dependencies;
            public readonly int UpdateOrder;
            public LimbMetadata(DependencyField[] dependencies, int updateOrder)
            {
                Dependencies = dependencies;
                UpdateOrder = updateOrder;
            }
        }

        public readonly struct DependencyField
        {
            public readonly FieldInfo Field;
            public readonly Type FieldType;
            public readonly Type ElementType;
            public readonly bool IsArray;
            public readonly bool IsOptional;
            public DependencyField(FieldInfo field, Type fieldType, Type elementType, bool isArray, bool isOptional)
            {
                Field = field;
                FieldType = fieldType;
                ElementType = elementType;
                IsArray = isArray;
                IsOptional = isOptional;
            }
        }

        static readonly Dictionary<Type, LimbMetadata> _cache = new();

        public static LimbMetadata GetMetadata(Type limbType)
        {
            if (_cache.TryGetValue(limbType, out var meta))
                return meta;

            meta = BuildMetadata(limbType);
            _cache[limbType] = meta;
            return meta;
        }

        static LimbMetadata BuildMetadata(Type type)
        {
            var deps = new List<DependencyField>();
            int order = 0;

            var current = type;
            while (current != null && current != typeof(object))
            {
                var fields = current.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<DependencyAttribute>() is { } dep)
                    {
                        bool isArray = field.FieldType.IsArray;
                        deps.Add(new DependencyField(
                            field: field,
                            fieldType: field.FieldType,
                            elementType: isArray ? field.FieldType.GetElementType() : null,
                            isArray: isArray,
                            isOptional: dep.Optional));
                    }
                }

                if (current.GetCustomAttribute<UpdateOrderAttribute>() is { } orderAttr)
                    order = orderAttr._order;

                current = current.BaseType;
            }

            return new LimbMetadata(deps.ToArray(), order);
        }
    }
}