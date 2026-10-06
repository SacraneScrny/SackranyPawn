
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SackranyPawn.Entities.Modules;
using UnityEngine;

namespace SackranyPawn.Cache
{
    internal static class HashBuilder
    {
        const int MaxDepth = 8;
        const uint CycleMarker = 0xDEADBEEFu;
        const uint ErrorMarker = 0xBADCAFEu;
        const int FallbackPrecision = 3;

        public static uint Begin() => 2166136261u; // FNV-1a offset

        public static uint Add(uint hash, uint data) => (hash ^ data) * 16777619u;

        public static uint BuildFromTemplates(IEnumerable<object> templates)
        {
            uint hash = Begin();
            if (templates == null) return hash;

            var list = new List<object>();
            foreach (var t in templates)
                if (t != null) list.Add(t);

            list.Sort((a, b) => string.Compare(
                a.GetType().FullName,
                b.GetType().FullName,
                StringComparison.Ordinal));

            foreach (var t in list)
            {
                var type = t.GetType();
                hash = AddString(hash, type.FullName);

                LimbKeyReflectionCache.HashKeyField[] keys;
                try
                {
                    keys = LimbKeyReflectionCache.GetHashKeys(type);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[HashBuilder] GetHashKeys failed for {type.FullName}: {e.Message}");
                    hash = Add(hash, ErrorMarker);
                    continue;
                }

                var visited = new HashSet<object>(RefEq.Instance);
                for (int i = 0; i < keys.Length; i++)
                {
                    object fieldValue;
                    try
                    {
                        fieldValue = keys[i].Field.GetValue(t);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[HashBuilder] GetValue failed for {type.FullName}.{keys[i].Field.Name}: {e.Message}");
                        hash = Add(hash, ErrorMarker);
                        continue;
                    }
                    hash = AddDeep(hash, fieldValue, keys[i].Attr, 0, visited);
                }
            }

            return hash;
        }

        static uint AddDeep(uint hash, object value, HashKeyAttribute attr, int depth, HashSet<object> visited)
        {
            try
            {
                if (depth > MaxDepth)
                {
                    Debug.LogWarning($"[HashBuilder] Nesting too deep (>{MaxDepth}), truncating.");
                    return Add(hash, CycleMarker);
                }

                if (value == null)
                    return Add(hash, 0u);

                if (value is string s)
                    return AddString(hash, s);

                if (value is IEnumerable enumerable)
                    return AddEnumerable(hash, enumerable, attr, depth, visited);

                var valueType = value.GetType();
                var nestedKeys = SafeGetKeys(valueType);
                if (nestedKeys.Length > 0)
                {
                    if (!valueType.IsValueType)
                    {
                        if (!visited.Add(value))
                        {
                            Debug.LogWarning($"[HashBuilder] Circular reference: {valueType.FullName}, truncating.");
                            return Add(hash, CycleMarker);
                        }
                        try
                        {
                            hash = AddString(hash, valueType.FullName);
                            for (int i = 0; i < nestedKeys.Length; i++)
                                hash = AddDeep(hash, SafeGetValue(nestedKeys[i], value, ref hash), nestedKeys[i].Attr, depth + 1, visited);
                            return hash;
                        }
                        finally { visited.Remove(value); }
                    }

                    hash = AddString(hash, valueType.FullName);
                    for (int i = 0; i < nestedKeys.Length; i++)
                        hash = AddDeep(hash, SafeGetValue(nestedKeys[i], value, ref hash), nestedKeys[i].Attr, depth + 1, visited);
                    return hash;
                }

                if ((attr?.IgnoreDefault ?? false) && IsDefaultValue(value, valueType))
                    return hash;

                return AddLeaf(hash, value, attr);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HashBuilder] Field skipped ({value?.GetType().FullName}): {e.Message}");
                return Add(hash, ErrorMarker);
            }
        }

        static uint AddEnumerable(uint hash, IEnumerable enumerable, HashKeyAttribute attr, int depth, HashSet<object> visited)
        {
            var items = new List<object>();
            foreach (var item in enumerable)
                items.Add(item);

            if ((attr?.IgnoreDefault ?? false) && items.Count == 0)
                return hash;

            var subs = new List<uint>(items.Count);
            for (int i = 0; i < items.Count; i++)
                subs.Add(AddDeep(Begin(), items[i], attr, depth + 1, visited));
            subs.Sort();

            hash = Add(hash, unchecked((uint)items.Count));
            for (int i = 0; i < subs.Count; i++)
                hash = Add(hash, subs[i]);
            return hash;
        }

        static uint AddLeaf(uint hash, object value, HashKeyAttribute attr)
        {
            switch (value)
            {
                case int v: return Add(hash, unchecked((uint)v));
                case uint v: return Add(hash, v);
                case long v: return AddLong(hash, unchecked((ulong)v));
                case ulong v: return AddLong(hash, v);
                case short v: return Add(hash, unchecked((uint)v));
                case byte v: return Add(hash, v);
                case bool v: return Add(hash, v ? 1u : 0u);
                case Enum v: return Add(hash, unchecked((uint)Convert.ToInt32(v)));
                case float v: return Add(hash, QuantizeSafe(v, attr?.Precision ?? FallbackPrecision));
                case double v: return AddDoubleSafe(hash, v, attr?.Precision ?? FallbackPrecision);
                case string v: return AddString(hash, v);
                case Vector2 v: return AddFloatN(hash, attr, v.x, v.y);
                case Vector3 v: return AddFloatN(hash, attr, v.x, v.y, v.z);
                case Vector4 v: return AddFloatN(hash, attr, v.x, v.y, v.z, v.w);
                case Vector2Int v: return AddIntN(hash, v.x, v.y);
                case Vector3Int v: return AddIntN(hash, v.x, v.y, v.z);
                case Quaternion v: return AddFloatN(hash, attr, v.x, v.y, v.z, v.w);
                case Color v: return AddFloatN(hash, attr, v.r, v.g, v.b, v.a);
                case Color32 v: return AddIntN(hash, v.r, v.g, v.b, v.a);
                default:
                    Debug.LogWarning($"[HashBuilder] Unsupported HashKey type: {value.GetType().FullName}, using ToString fallback.");
                    hash = AddString(hash, value.GetType().FullName);
                    return AddString(hash, value.ToString() ?? string.Empty);
            }
        }

        sealed class RefEq : IEqualityComparer<object>
        {
            public static readonly RefEq Instance = new();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }

        static bool IsDefaultValue(object value, Type type)
        {
            try
            {
                if (!type.IsValueType) return value == null;
                var def = Activator.CreateInstance(type);
                return value.Equals(def);
            }
            catch { return false; }
        }

        static LimbKeyReflectionCache.HashKeyField[] SafeGetKeys(Type t)
        {
            try { return LimbKeyReflectionCache.GetHashKeys(t); }
            catch { return Array.Empty<LimbKeyReflectionCache.HashKeyField>(); }
        }

        static object SafeGetValue(LimbKeyReflectionCache.HashKeyField hk, object owner, ref uint hash)
        {
            try { return hk.Field.GetValue(owner); }
            catch (Exception e)
            {
                Debug.LogWarning($"[HashBuilder] GetValue failed for {owner.GetType().FullName}.{hk.Field.Name}: {e.Message}");
                hash = Add(hash, ErrorMarker);
                return null;
            }
        }

        static uint AddLong(uint hash, ulong v)
        {
            hash = Add(hash, (uint)(v & 0xFFFFFFFF));
            hash = Add(hash, (uint)(v >> 32));
            return hash;
        }

        static uint AddDoubleSafe(uint hash, double v, int precision)
        {
            if (precision <= 0) precision = FallbackPrecision;
            if (double.IsNaN(v) || double.IsInfinity(v)) return Add(hash, 0x7FC00000u);
            if (v == 0.0) v = 0.0;
            long quantized = (long)Math.Round(v * Math.Pow(10, precision));
            return AddLong(hash, unchecked((ulong)quantized));
        }

        static uint AddFloatN(uint hash, HashKeyAttribute attr, params float[] xs)
        {
            int p = attr?.Precision ?? FallbackPrecision;
            for (int i = 0; i < xs.Length; i++) hash = Add(hash, QuantizeSafe(xs[i], p));
            return hash;
        }

        static uint AddIntN(uint hash, params int[] xs)
        {
            for (int i = 0; i < xs.Length; i++) hash = Add(hash, unchecked((uint)xs[i]));
            return hash;
        }

        static uint AddString(uint hash, string str)
        {
            if (str == null) return Add(hash, 0u);
            for (int i = 0; i < str.Length; i++)
                hash = Add(hash, str[i]);
            return hash;
        }

        static uint QuantizeSafe(float v, int precision)
        {
            if (precision <= 0) precision = FallbackPrecision;
            if (float.IsNaN(v) || float.IsInfinity(v)) return 0x7FC00000u;
            if (v == 0f) v = 0f;
            return unchecked((uint)Mathf.RoundToInt(v * Mathf.Pow(10, precision)));
        }
    }
}