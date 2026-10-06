using System;
using System.Collections.Generic;
using System.Linq;

namespace SackranyPawn.Cache
{
    public static class TeamId
    {
        private static readonly Dictionary<string, int> _toId = new();
        private static int _nextId = 1;
        
        internal static void Clear()
        {
            _toId.Clear();
            _nextId = 1;
        }

        public static int Get(string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
                return -1;

            var copy = keywords.ToArray();
            Array.Sort(copy, StringComparer.Ordinal);

            string key = string.Join('\0', copy);

            if (_toId.TryGetValue(key, out int id))
                return id;

            id = _nextId++;
            _toId[key] = id;

            return id;
        }
    }
}