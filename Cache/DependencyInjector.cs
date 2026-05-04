using System;
using System.Collections.Generic;

using SackranyPawn.Entities.Modules;

using UnityEngine;

namespace SackranyPawn.Cache
{
    internal static class DependencyInjector
    {
        static readonly List<object> _buffer = new(16);
        
        public static bool InjectDependencies(object target, Body body)
        {
            var meta = LimbReflectionCache.GetMetadata(target.GetType());

            foreach (var dep in meta.Dependencies)
            {
                if (dep.IsArray)
                {
                    _buffer.Clear();
                    body.GetAllAssignable(dep.ElementType, _buffer);

                    if (_buffer.Count == 0)
                        body.GetAllAssignableComponents(dep.ElementType, _buffer);

                    if (_buffer.Count == 0 && !dep.IsOptional)
                        return false;

                    var array = Array.CreateInstance(dep.ElementType, _buffer.Count);
                    for (int i = 0; i < _buffer.Count; i++)
                    {
                        if (!dep.ElementType.IsInstanceOfType(_buffer[i]))
                            continue;
                        array.SetValue(_buffer[i], i);
                    }

                    dep.Field.SetValue(target, array);
                }
                else
                {
                    if (!TryResolve(dep.FieldType, body, out var resolved))
                    {
                        if (!dep.IsOptional) return false;
                        continue;
                    }

                    dep.Field.SetValue(target, resolved);
                }
            }

            return true;
        }
        public static bool TryResolve(Type type, Body body, out object result)
        {
            if (body.TryGet(type, out var module, tryAssignable: true))
            {
                result = module;
                return true;
            }

            if (typeof(Component).IsAssignableFrom(type))
            {
                var unit = body.Pawn;
                var comp = unit.GetComponent(type);
                if (comp == null)
                    comp = unit.GetComponentInChildren(type, true);
                if (comp == null)
                    comp = unit.GetComponentInParent(type, true);
                if (comp != null)
                {
                    result = comp;
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}