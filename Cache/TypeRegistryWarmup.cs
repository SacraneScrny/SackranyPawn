using System;
using System.Collections.Generic;
using System.Reflection;

using SackranyPawn.Entities;
using SackranyPawn.Entities.Modules;
using SackranyPawn.Traits.Conditions;
using SackranyPawn.Traits.Fluxes.Entities;
using SackranyPawn.Traits.PawnEvents;
using SackranyPawn.Traits.PawnTags;
using SackranyPawn.Traits.Stats;

using UnityEngine;

namespace SackranyPawn.Cache
{
    internal static class TypeRegistryWarmup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Warmup()
        {
            TypeRegistry<Limb>.Reset();
            TypeRegistry<IStat>.Reset();
            TypeRegistry<ICondition>.Reset();
            TypeRegistry<IEvent>.Reset();
            TypeRegistry<IPawnTag>.Reset();
            TypeRegistry<Flux>.Reset();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Register<Limb>(assemblies);
            Register<IStat>(assemblies);
            Register<ICondition>(assemblies);
            Register<IEvent>(assemblies);
            Register<IPawnTag>(assemblies);
            Register<Flux>(assemblies);
        }

        static void Register<TBase>(Assembly[] assemblies) where TBase : class
        {
            var baseType = typeof(TBase);
            var found = new List<Type>(64);

            foreach (var assembly in assemblies)
            {
                CollectFromAssembly(assembly, baseType, found);
                CollectWarmupAttributes(assembly, baseType, found);
            }
            found.Sort(static (a, b) =>
                string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));

            for (int i = found.Count - 1; i > 0; i--)
                if (found[i] == found[i - 1]) found.RemoveAt(i);

            foreach (var type in found)
                TypeRegistry<TBase>.GetOrRegister(type);

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[TypeRegistryWarmup] {typeof(TBase).Name}: {found.Count} types registered.");
            #endif
        }

        static void CollectFromAssembly(Assembly assembly, Type baseType, List<Type> found)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // Partial load — use whatever resolved before the failure.
                types = e.Types;
            }

            if (types == null) return;

            foreach (var type in types)
            {
                if (type == null) continue;
                if (type.IsAbstract || type.IsInterface) continue;
                if (type.IsGenericTypeDefinition) continue;

                if (!baseType.IsAssignableFrom(type)) continue;

                found.Add(type);
            }
        }

        static void CollectWarmupAttributes(Assembly assembly, Type baseType, List<Type> found)
        {
            IEnumerable<WarmupTypeAttribute> attrs;
            try
            {
                attrs = assembly.GetCustomAttributes<WarmupTypeAttribute>();
            }
            catch
            {
                return;
            }

            foreach (var attr in attrs)
            {
                var type = attr.Type;
                if (type == null) continue;
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) continue;
                if (!baseType.IsAssignableFrom(type)) continue;
                found.Add(type);
            }
        }
    }
}