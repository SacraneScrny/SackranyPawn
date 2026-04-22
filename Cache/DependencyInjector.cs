using System;

using Sackrany.Actor.Modules;

using UnityEngine;

namespace SackranyPawn.Cache
{
    public static class DependencyInjector
    {
        public static bool Inject(object target, ModulesController modules)
        {
            return InjectDependencies(target, modules);
        }
        public static bool InjectDependencies(object target, ModulesController modules)
        {
            var meta = LimbReflectionCache.GetMetadata(target.GetType());

            foreach (var dep in meta.Dependencies)
            {
                if (dep.IsArray)
                {
                    var found = modules.GetAllAssignable(dep.ElementType);
                    if ((found == null || found.Length == 0) && !dep.IsOptional)
                        return false;

                    var array = Array.CreateInstance(dep.ElementType, found.Length);
                    for (int i = 0; i < found.Length; i++)
                        array.SetValue(found[i], i);

                    dep.Field.SetValue(target, array);
                }
                else
                {
                    if (!TryResolve(dep.FieldType, modules, out var resolved))
                    {
                        if (!dep.IsOptional) return false;
                        continue;
                    }

                    dep.Field.SetValue(target, resolved);
                }
            }

            return true;
        }
        public static bool TryResolve(Type type, ModulesController modules, out object result)
        {
            if (modules.TryGet(type, out var module, tryAssignable: true))
            {
                result = module;
                return true;
            }

            if (typeof(Component).IsAssignableFrom(type))
            {
                var unit = modules.Unit;
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