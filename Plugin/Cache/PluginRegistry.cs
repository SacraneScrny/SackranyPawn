#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SackranyPawn.Plugin.Entities;

using UnityEngine;

namespace SackranyPawn.Plugin.Cache
{
    internal static class PluginRegistry
    {
        static readonly Dictionary<Type, object> _map = new();
        static readonly List<Action> _refreshers = new();

        sealed class PluginEntry
        {
            public Type Type;
            public object Instance;
            public int Order;
            public Type? Before;
            public Type? After;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _map.Clear();

            var pluginTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            var entries = new List<PluginEntry>();

            foreach (var type in pluginTypes)
            {
                var instance = Activator.CreateInstance(type);
                if (instance is not IPlugin plugin)
                    continue;

                var beforeAttr = type.GetCustomAttribute<InjectBeforePluginAttribute>();
                var afterAttr = type.GetCustomAttribute<InjectAfterPluginAttribute>();

                entries.Add(new PluginEntry
                {
                    Type = type,
                    Instance = instance!,
                    Order = plugin.Order,
                    Before = beforeAttr?.Key,
                    After = afterAttr?.Key
                });
            }

            foreach (var entry in entries)
            {
                foreach (var iface in entry.Type.GetInterfaces())
                {
                    if (!typeof(IPlugin).IsAssignableFrom(iface))
                        continue;

                    if (!_map.TryGetValue(iface, out var listObj))
                    {
                        var listType = typeof(List<>).MakeGenericType(iface);
                        listObj = Activator.CreateInstance(listType)!;
                        _map[iface] = listObj;
                    }
                }
            }

            foreach (var kvp in _map.ToArray())
            {
                var iface = kvp.Key;
                var listObj = kvp.Value;
                var listType = listObj.GetType();

                var ifaceEntries = entries
                    .Where(e => iface.IsAssignableFrom(e.Type))
                    .ToList();

                var ordered = TopologicalSort(ifaceEntries);

                var add = listType.GetMethod("Add")!;
                foreach (var entry in ordered)
                    add.Invoke(listObj, new[] { entry.Instance });
            }

            foreach (var refresh in _refreshers)
                refresh();
        }

        static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
        }
        static List<PluginEntry> TopologicalSort(List<PluginEntry> entries)
        {
            if (entries.Count <= 1)
                return entries;

            var byType = entries.ToDictionary(x => x.Type, x => x);
            var outgoing = new Dictionary<Type, HashSet<Type>>();
            var incomingCount = new Dictionary<Type, int>();

            foreach (var entry in entries)
            {
                outgoing[entry.Type] = new HashSet<Type>();
                incomingCount[entry.Type] = 0;
            }

            void AddEdge(Type from, Type to)
            {
                if (!byType.ContainsKey(from) || !byType.ContainsKey(to))
                    return;

                if (outgoing[from].Add(to))
                    incomingCount[to]++;
            }

            foreach (var entry in entries)
            {
                if (entry.Before != null)
                    AddEdge(entry.Type, entry.Before);

                if (entry.After != null)
                    AddEdge(entry.After, entry.Type);
            }

            var comparer = Comparer<PluginEntry>.Create((a, b) =>
            {
                var orderCmp = a.Order.CompareTo(b.Order);
                if (orderCmp != 0) return orderCmp;

                return string.Compare(a.Type.FullName, b.Type.FullName, StringComparison.Ordinal);
            });

            var ready = new SortedSet<PluginEntry>(comparer);
            foreach (var entry in entries)
            {
                if (incomingCount[entry.Type] == 0)
                    ready.Add(entry);
            }

            var result = new List<PluginEntry>(entries.Count);

            while (ready.Count > 0)
            {
                var current = ready.Min!;
                ready.Remove(current);
                result.Add(current);

                foreach (var nextType in outgoing[current.Type])
                {
                    incomingCount[nextType]--;
                    if (incomingCount[nextType] == 0)
                        ready.Add(byType[nextType]);
                }
            }

            if (result.Count != entries.Count)
            {
                var remaining = entries
                    .Where(e => !result.Contains(e))
                    .OrderBy(e => e.Order)
                    .ThenBy(e => e.Type.FullName)
                    .ToList();

                Debug.LogError(
                    $"PluginRegistry: cycle detected in plugin ordering. Falling back to Order for remaining {remaining.Count} plugins.");

                result.AddRange(remaining);
            }

            return result;
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