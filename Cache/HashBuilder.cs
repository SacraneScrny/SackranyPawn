using System;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Entities.Modules;

using UnityEngine;

namespace SackranyPawn.Cache
{
    internal static class HashBuilder
    {
        public static uint Begin() => 2166136261u; // FNV-1a offset

        public static uint Add(uint hash, uint data) => (hash ^ data) * 16777619u;

        public static uint BuildFromTemplates(IEnumerable<object> templates)
        {
            uint hash = Begin();
            foreach (var t in templates.OrderBy(t => t.GetType().FullName))
            {
                var type = t.GetType();
                hash = AddString(hash, type.FullName);

                foreach (var hk in LimbKeyReflectionCache.GetHashKeys(type))
                    hash = AddValue(hash, hk.Field.GetValue(t), hk.Attr);
            }
            return hash;
        }
        static uint AddValue(uint hash, object value, HashKeyAttribute attr)
        {
            if (value == null)
                return Add(hash, 0u);

            if (attr.IgnoreDefault && value.Equals(GetDefault(value.GetType())))
                return hash;

            return value switch
            {
                int v => Add(hash, unchecked((uint)v)),
                uint v => Add(hash, v),
                long v => AddLong(hash, unchecked((ulong)v)),
                ulong v => AddLong(hash, v),
                short v => Add(hash, unchecked((uint)v)),
                byte v => Add(hash, v),
                bool v => Add(hash, v ? 1u : 0u),
                Enum v => Add(hash, unchecked((uint)Convert.ToInt32(v))),

                float v => Add(hash, Quantize(v, attr.Precision)),
                double v => AddDouble(hash, v, attr.Precision),

                string v => AddString(hash, v),

                Vector2 v => AddFloat2(hash, v.x, v.y, attr.Precision),
                Vector3 v => AddFloat3(hash, v.x, v.y, v.z, attr.Precision),
                Vector4 v => AddFloat4(hash, v.x, v.y, v.z, v.w, attr.Precision),
                Vector2Int v => AddInt2(hash, v.x, v.y),
                Vector3Int v => AddInt3(hash, v.x, v.y, v.z),
                Quaternion v => AddFloat4(hash, v.x, v.y, v.z, v.w, attr.Precision),
                Color v => AddFloat4(hash, v.r, v.g, v.b, v.a, attr.Precision),
                Color32 v => AddInt4(hash, v.r, v.g, v.b, v.a),

                _ => throw new Exception(
                    $"Unsupported HashKey type: {value.GetType()}")
            };
        }
        
        static object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
        static uint AddLong(uint hash, ulong v)
        {
            hash = Add(hash, (uint)(v & 0xFFFFFFFF));
            hash = Add(hash, (uint)(v >> 32));
            return hash;
        }
        static uint AddDouble(uint hash, double v, int precision)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
                throw new Exception("Invalid double value for hashing");
            if (v == 0.0) v = 0.0;
            long quantized = (long)Math.Round(v / Math.Pow(10, precision));
            return AddLong(hash, unchecked((ulong)quantized));
        }
        static uint AddFloat2(uint hash, float x, float y, int p)
        {
            hash = Add(hash, Quantize(x, p));
            hash = Add(hash, Quantize(y, p));
            return hash;
        }
        static uint AddFloat3(uint hash, float x, float y, float z, int p)
        {
            hash = Add(hash, Quantize(x, p));
            hash = Add(hash, Quantize(y, p));
            hash = Add(hash, Quantize(z, p));
            return hash;
        }
        static uint AddFloat4(uint hash, float x, float y, float z, float w, int p)
        {
            hash = Add(hash, Quantize(x, p));
            hash = Add(hash, Quantize(y, p));
            hash = Add(hash, Quantize(z, p));
            hash = Add(hash, Quantize(w, p));
            return hash;
        }
        static uint AddInt2(uint hash, int x, int y)
        {
            hash = Add(hash, unchecked((uint)x));
            hash = Add(hash, unchecked((uint)y));
            return hash;
        }
        static uint AddInt3(uint hash, int x, int y, int z)
        {
            hash = Add(hash, unchecked((uint)x));
            hash = Add(hash, unchecked((uint)y));
            hash = Add(hash, unchecked((uint)z));
            return hash;
        }
        static uint AddInt4(uint hash, int x, int y, int z, int w)
        {
            hash = Add(hash, unchecked((uint)x));
            hash = Add(hash, unchecked((uint)y));
            hash = Add(hash, unchecked((uint)z));
            hash = Add(hash, unchecked((uint)w));
            return hash;
        }
        static uint AddString(uint hash, string str)
        {
            if (str == null) return Add(hash, 0u);
            for (int i = 0; i < str.Length; i++)
                hash = Add(hash, str[i]);
            return hash;
        }
        static uint Quantize(float v, int precision)
        {
            if (precision <= 0f)
                throw new Exception("Float HashKey requires precision");

            if (float.IsNaN(v) || float.IsInfinity(v))
                throw new Exception("Invalid float value for hashing");

            if (v == 0f) v = 0f;

            return unchecked((uint)Mathf.RoundToInt(v / Mathf.Pow(10, precision)));
        }
    }
}