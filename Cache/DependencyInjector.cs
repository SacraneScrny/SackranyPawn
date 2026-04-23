using System;

using SackranyPawn.Entities.Modules;

using UnityEngine;

namespace SackranyPawn.Cache
{
    public static class DependencyInjector
    {
        public static bool Inject(object target, Body body)
        {
            return InjectDependencies(target, body);
        }
        public static bool InjectDependencies(object target, Body body)
        {
            var meta = LimbReflectionCache.GetMetadata(target.GetType());

            foreach (var dep in meta.Dependencies)
            {
                if (dep.IsArray)
                {
                    object[] found = body.GetAllAssignable(dep.ElementType);
                    if ((found == null || found.Length == 0))
                    {
                        found = body.GetAllAssignableComponents(dep.ElementType);
                    }

                    if ((found == null || found.Length == 0) && !dep.IsOptional)
                        return false;

                    var array = Array.CreateInstance(dep.ElementType, found.Length);
                    for (int i = 0; i < found.Length; i++)
                    {
                        if (!dep.ElementType.IsInstanceOfType(found[i]))
                            continue;
                        array.SetValue(found[i], i);
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