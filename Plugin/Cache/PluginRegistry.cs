using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Plugin.Entities;

using UnityEngine;

namespace SackranyPawn.Plugin.Cache
{
    internal static class PluginRegistry
    {
        static readonly Dictionary<Type, object> _map = new ();
        static readonly List<Action> _refreshers = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _map.Clear();

            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
            
            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);

                foreach (var iface in type.GetInterfaces())
                {
                    if (!typeof(IPlugin).IsAssignableFrom(iface))
                        continue;
                    if (!_map.TryGetValue(iface, out var list))
                    {
                        var listType = typeof(List<>).MakeGenericType(iface);
                        list = Activator.CreateInstance(listType);
                        _map[iface] = list;
                    }
                    list.GetType().GetMethod("Add")!.Invoke(list, new[] { instance });
                }
            }
            
            foreach (var refresh in _refreshers)
                refresh();
        }

        public static class Get<T>
        {
            public static T[] Value = Array.Empty<T>();

            static Get()
            {
                Action refresh = static () =>
                {
                    Value = _map.TryGetValue(typeof(T), out var list)
                        ? ((List<T>)list).ToArray()
                        : Array.Empty<T>();
                };

                _refreshers.Add(refresh);
                refresh();
            }
        }
    }
}