using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using SackranyPawn.Components;

namespace SackranyPawn.Cache
{
    internal static class ArchetypeCache
    {
        static readonly Dictionary<Type, FieldInfo> _controllerFields = new();
        static readonly Dictionary<Type, FieldInfo> _defaultFields = new();

        public static uint GetHash(Pawn pawn)
        {
            var controller = GetBody(pawn);
            if (controller == null) return 0u;
            return HashBuilder.BuildFromTemplates(CollectTemplates(controller));
        }
        static object GetBody(Pawn pawn)
        {
            var unitType = pawn.GetType();
            if (!_controllerFields.TryGetValue(unitType, out var fi))
            {
                fi = unitType.GetField("Body",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _controllerFields[unitType] = fi;
            }
            return fi?.GetValue(pawn);
        }
        static List<object> CollectTemplates(object body)
        {
            var list = new List<object>(16);
            var controllerType = body.GetType();

            if (!_defaultFields.TryGetValue(controllerType, out var fi))
            {
                fi = controllerType.GetField("Limbs",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _defaultFields[controllerType] = fi;
            }

            if (fi?.GetValue(body) is IEnumerable templates)
                foreach (var t in templates)
                    if (t != null) list.Add(t);

            return list;
        }
    }
}