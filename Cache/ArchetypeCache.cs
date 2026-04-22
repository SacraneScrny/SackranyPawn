using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Sackrany.Actor.UnitMono;

namespace SackranyPawn.Cache
{
    static class ArchetypeCache
    {
        static readonly Dictionary<Type, FieldInfo> _controllerFields = new();
        static readonly Dictionary<Type, FieldInfo> _defaultFields = new();

        public static uint GetHash(Unit unit)
        {
            var controller = GetController(unit);
            if (controller == null) return 0u;
            return HashBuilder.BuildFromTemplates(CollectTemplates(controller));
        }
        static object GetController(Unit unit)
        {
            var unitType = unit.GetType();
            if (!_controllerFields.TryGetValue(unitType, out var fi))
            {
                fi = unitType.GetField("Controller",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _controllerFields[unitType] = fi;
            }
            return fi?.GetValue(unit);
        }
        static List<object> CollectTemplates(object controller)
        {
            var list = new List<object>(16);
            var controllerType = controller.GetType();

            if (!_defaultFields.TryGetValue(controllerType, out var fi))
            {
                fi = controllerType.GetField("Default",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _defaultFields[controllerType] = fi;
            }

            if (fi?.GetValue(controller) is IEnumerable templates)
                foreach (var t in templates)
                    if (t != null) list.Add(t);

            return list;
        }
    }
}