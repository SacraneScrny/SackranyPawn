using System;
using System.Collections.Generic;

using SackranyPawn.Cache;
using SackranyPawn.Traits.Fluxes.Entities;

using UnityEngine;

namespace SackranyPawn.Traits.Fluxes.Cache
{
    public static class FluxRegistry
    {
        public static int Count => TypeRegistry<Flux>.Count;
        public static int GetId<T>() where T : Flux => TypeRegistry<Flux>.Id<T>.Value;
        public static int GetId(Type type) => TypeRegistry<Flux>.GetOrRegister(type);
        public static Type GetTypeById(int id) => TypeRegistry<Flux>.GetTypeById(id);
        internal static int LookupId(Type type) => TypeRegistry<Flux>.GetOrRegister(type);
        
        static readonly Dictionary<Type, Flux> _templates = new();
        [RuntimeInitializeOnLoadMethod]
        static void ResetTemplates() => _templates.Clear();
        
        internal static Flux GetTemplate<T>() where T : Flux, new()
        {
            if (!_templates.TryGetValue(typeof(T), out var template))
            {
                template = new T();
                _templates[typeof(T)] = template;
            }
            return template;
        }
    }
}