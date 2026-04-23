using System;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Cache;
using SackranyPawn.Components;
using SackranyPawn.Entities;
using SackranyPawn.Traits.PawnTags;

using UnityEngine;

namespace SackranyPawn.Managers
{
    public static class PawnRegister
    {
        static readonly Dictionary<TeamInfo, Dictionary<int, Pawn>> _cachedTeams = new();
        static readonly Dictionary<int, Pawn> _cachedPawns = new();
        static readonly Dictionary<PawnArchetype, Dictionary<int, Pawn>> _cachedArchetypes = new();
        static readonly Dictionary<int, Dictionary<int, Pawn>> _cachedTags = new();
        static readonly List<Pawn> _cachedArray = new();
        
        static readonly Dictionary<int, int> _hashToIndex = new();
        static readonly Dictionary<int, PawnHandlers> _handlers = new();
        readonly struct PawnHandlers
        {
            public readonly Action<int> OnTagAdded;
            public readonly Action<int> OnTagRemoved;
            public PawnHandlers(Action<int> onTagAdded, Action<int> onTagRemoved)
            {
                OnTagAdded   = onTagAdded;
                OnTagRemoved = onTagRemoved;
            }
        }
        
        public static IReadOnlyList<Pawn> RegisteredPawns => _cachedArray;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _cachedTeams.Clear();
            _cachedPawns.Clear();
            _cachedArchetypes.Clear();
            _cachedTags.Clear();
            _cachedArray.Clear();
            _hashToIndex.Clear();
            _handlers.Clear();
        }

        public static bool RegisterPawn(Pawn unit)
        {
            if (_cachedPawns.ContainsKey(unit.Hash)) return false;

            if (!_cachedArchetypes.TryGetValue(unit.Archetype, out var archetypes))
            {
                archetypes = new();
                _cachedArchetypes.Add(unit.Archetype, archetypes);
            }

            RegisterTeam(unit);
            RegisterTags(unit);
            archetypes.TryAdd(unit.Hash, unit);
            _cachedPawns.Add(unit.Hash, unit);
            
            _hashToIndex[unit.Hash] = _cachedArray.Count;
            _cachedArray.Add(unit);

            var handlers = new PawnHandlers(
                onTagAdded:   id => OnPawnTagAdded(unit, id),
                onTagRemoved: id => OnPawnTagRemoved(unit, id)
            );
            _handlers[unit.Hash] = handlers;
            unit.OnStartWorking    += HandlePawnStarted;
            unit.Tag.OnTagAdded    += handlers.OnTagAdded;
            unit.Tag.OnTagRemoved  += handlers.OnTagRemoved;

            OnPawnRegistered?.Invoke(unit);
            return true;
        }
        static bool RegisterTeam(Pawn unit)
        {
            if (!_cachedTeams.TryGetValue(unit.Team, out var team))
            {
                team = new();
                _cachedTeams.Add(unit.Team, team);
            }
            return team.TryAdd(unit.Hash, unit);
        }
        static void RegisterTags(Pawn unit)
        {
            foreach (var id in unit.Tag.GetIds())
                AddToTagIndex(unit, id);
        }

        static void AddToTagIndex(Pawn unit, int tagId)
        {
            if (!_cachedTags.TryGetValue(tagId, out var bucket))
            {
                bucket = new();
                _cachedTags.Add(tagId, bucket);
            }
            bucket.TryAdd(unit.Hash, unit);
        }
        static void RemoveFromTagIndex(Pawn unit, int tagId)
        {
            if (_cachedTags.TryGetValue(tagId, out var bucket))
                bucket.Remove(unit.Hash);
        }

        static void OnPawnTagAdded(Pawn unit, int tagId) => AddToTagIndex(unit, tagId);
        static void OnPawnTagRemoved(Pawn unit, int tagId) => RemoveFromTagIndex(unit, tagId);

        public static bool UnregisterPawn(Pawn unit)
        {
            if (!_cachedPawns.ContainsKey(unit.Hash)) return false;

            UnregisterTeam(unit);
            UnregisterTags(unit);
            
            if (_cachedArchetypes.TryGetValue(unit.Archetype, out var archetypes))
                archetypes.Remove(unit.Hash);
            _cachedPawns.Remove(unit.Hash);

            int idx  = _hashToIndex[unit.Hash];
            int last = _cachedArray.Count - 1;
            if (idx != last)
            {
                var swapped = _cachedArray[last];
                _cachedArray[idx]       = swapped;
                _hashToIndex[swapped.Hash] = idx;
            }
            _cachedArray.RemoveAt(last);
            _hashToIndex.Remove(unit.Hash);

            if (_handlers.TryGetValue(unit.Hash, out var handlers))
            {
                unit.OnStartWorking -= HandlePawnStarted;
                unit.Tag.OnTagAdded -= handlers.OnTagAdded;
                unit.Tag.OnTagRemoved -= handlers.OnTagRemoved;
                _handlers.Remove(unit.Hash);
            }

            OnPawnUnregistered?.Invoke(unit);
            return true;
        }
        static bool UnregisterTeam(Pawn unit)
        {
            if (!_cachedTeams.TryGetValue(unit.Team, out var team)) return false;
            return team.Remove(unit.Hash);
        }
        static void UnregisterTags(Pawn unit)
        {
            foreach (var id in unit.Tag.GetIds())
                RemoveFromTagIndex(unit, id);
        }

        public static bool HasPawns(Func<Pawn, bool> cond)
        {
            foreach (var unit in _cachedArray)
                if (cond(unit)) return true;
            return false;
        }
        public static bool HasPawnsWithTag<T>() where T : IPawnTag
            => _cachedTags.TryGetValue(TypeRegistry<IPawnTag>.Id<T>.Value, out var b) && b.Count > 0;

        #region GET
        public static Pawn GetPawn(Func<Pawn, bool> cond)
        {
            for (var i = 0; i < _cachedArray.Count; i++)
            {
                var unit = _cachedArray[i];
                if (cond(unit)) return unit;
            }
            return null;
        }
        public static Pawn GetPawnWithTag<T>() where T : IPawnTag
        {
            int id = TypeRegistry<IPawnTag>.Id<T>.Value;
            if (!_cachedTags.TryGetValue(id, out var bucket)) return null;
            foreach (var kvp in bucket)
                if (kvp.Value.IsActive) return kvp.Value;
            return null;
        }
        public static Pawn GetPawnWithTag<T>(Func<Pawn, bool> cond) where T : IPawnTag
        {
            int id = TypeRegistry<IPawnTag>.Id<T>.Value;
            if (!_cachedTags.TryGetValue(id, out var bucket)) return null;
            foreach (var kvp in bucket)
                if (kvp.Value.IsActive && cond(kvp.Value)) return kvp.Value;
            return null;
        }

        public static bool TryGetPawn(Func<Pawn, bool> cond, out Pawn value)
        {
            foreach (var unit in _cachedArray)
                if (cond(unit))
                {
                    value = unit;
                    return true;
                }
            value = null;
            return false;
        }
        public static bool TryGetPawn(TeamInfo team, Func<Pawn, bool> cond, out Pawn value)
        {
            if (!_cachedTeams.TryGetValue(team, out var teams))
            {
                value = null;
                return false;
            }
            foreach (var unit in teams)
                if (cond(unit.Value))
                {
                    value = unit.Value;
                    return true;
                }
            value = null;
            return false;
        }
        public static bool TryGetPawn(TeamInfo team, out Pawn value)
        {
            if (!_cachedTeams.TryGetValue(team, out var teams))
            {
                value = null;
                return false;
            }
            value = teams.FirstOrDefault().Value;
            return value != null;
        }
        public static bool TryGetPawnWithTag<T>(out Pawn value) where T : IPawnTag
        {
            value = GetPawnWithTag<T>();
            return value != null;
        }
        public static bool TryGetPawnWithTag<T>(Func<Pawn, bool> cond, out Pawn value) where T : IPawnTag
        {
            value = GetPawnWithTag<T>(cond);
            return value != null;
        }

        public static bool TryGetPawns(Func<Pawn, bool> cond, out Pawn[] value)
        {
            value = GetAllPawns(cond).ToArray();
            return value.Length > 0;
        }
        public static bool TryGetPawns(TeamInfo team, Func<Pawn, bool> cond, out Pawn[] value)
        {
            value = GetAllPawns(team, cond).ToArray();
            return value.Length > 0;
        }
        public static bool TryGetPawns(TeamInfo team, out Pawn[] value)
        {
            value = GetAllPawns(team).ToArray();
            return value.Length > 0;
        }

        public static bool TryGetPawns(Func<Pawn, bool> cond, out List<Pawn> value)
        {
            value = GetAllPawns(cond).ToList();
            return value.Count > 0;
        }
        public static bool TryGetPawns(TeamInfo team, Func<Pawn, bool> cond, out List<Pawn> value)
        {
            value = GetAllPawns(team, cond).ToList();
            return value.Count > 0;
        }
        public static bool TryGetPawns(TeamInfo team, out List<Pawn> value)
        {
            value = GetAllPawns(team).ToList();
            return value.Count > 0;
        }

        public static bool TryGetPawnsWithTag<T>(out Pawn[] value) where T : IPawnTag
        {
            value = GetAllPawnsWithTag<T>().ToArray();
            return value.Length > 0;
        }
        public static bool TryGetPawnsWithTag<T>(Func<Pawn, bool> cond, out Pawn[] value) where T : IPawnTag
        {
            value = GetAllPawnsWithTag<T>(cond).ToArray();
            return value.Length > 0;
        }

        public static IReadOnlyList<Pawn> GetAllPawns() => _cachedArray;
        public static IReadOnlyList<Pawn> GetAllPawns(TeamInfo team) => 
            !_cachedTeams.TryGetValue(team, out var teams)
                ? Array.Empty<Pawn>()
                : teams.Select(x => x.Value).ToList().AsReadOnly();
        public static IEnumerable<Pawn> GetAllPawns(Func<Pawn, bool> cond) => _cachedArray.Where(cond);
        public static IEnumerable<Pawn> GetAllPawns(TeamInfo team, Func<Pawn, bool> cond) => 
            !_cachedTeams.TryGetValue(team, out var teams)
                ? Array.Empty<Pawn>()
                : teams.Select(x => x.Value).Where(cond);
        public static IEnumerable<Pawn> GetAllPawns(PawnArchetype archetype) => 
            _cachedArchetypes.TryGetValue(archetype, out var archetypes)
                ? archetypes.Select(x => x.Value)
                : Array.Empty<Pawn>();

        public static IEnumerable<Pawn> GetAllPawnsWithTag<T>() where T : IPawnTag
        {
            int id = TypeRegistry<IPawnTag>.Id<T>.Value;
            if (!_cachedTags.TryGetValue(id, out var bucket)) return Array.Empty<Pawn>();
            return bucket.Values.Where(u => u.IsActive);
        }
        public static IEnumerable<Pawn> GetAllPawnsWithTag<T>(Func<Pawn, bool> cond) where T : IPawnTag
        {
            int id = TypeRegistry<IPawnTag>.Id<T>.Value;
            if (!_cachedTags.TryGetValue(id, out var bucket)) return Array.Empty<Pawn>();
            return bucket.Values.Where(u => u.IsActive && cond(u));
        }
        #endregion

        public static event Action<Pawn> OnPawnRegistered;
        public static event Action<Pawn> OnPawnUnregistered;
        public static event Action<Pawn> OnPawnStarted;

        static void HandlePawnStarted(Pawn unit) => OnPawnStarted?.Invoke(unit);
    }
}