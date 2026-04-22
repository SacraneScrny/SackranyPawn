using System;
using System.Collections.Generic;

using SackranyPawn.Components;

using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace SackranyPawn.Managers
{
    public static class PawnSpatial
    {
        struct PawnSpatialUpdateSystem { }
        
        public static float CellSize { get; private set; } = 10f;

        static readonly Dictionary<CellKey, List<Pawn>> _cells = new();
        static readonly Dictionary<int, CellKey> _pawnCell = new();
        static readonly Dictionary<int, Pawn> _pawns = new();
        
        static readonly List<Pawn> _pawnList = new();
        static readonly Dictionary<int, int> _pawnListIndex = new();

        static float _invCellSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            _cells.Clear();
            _pawns.Clear();
            _pawnList.Clear();
            _pawnListIndex.Clear();
            _pawnCell.Clear();
            
            _invCellSize = 1f / CellSize;
            
            var registered = PawnRegister.RegisteredPawns;
            for (int i = 0; i < registered.Count; i++)
                RegisterInternal(registered[i]);

            PawnRegister.OnPawnRegistered += RegisterInternal;
            PawnRegister.OnPawnUnregistered += UnregisterInternal;
            
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            PlayerLoopUtils.InsertAfter<Update.ScriptRunBehaviourUpdate>(ref loop, new PlayerLoopSystem
            {
                type = typeof(PawnSpatialUpdateSystem),
                updateDelegate = Tick
            });

            PlayerLoop.SetPlayerLoop(loop);

            Application.quitting -= CleanUp;
            Application.quitting += CleanUp;
        }

        static void CleanUp()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopUtils.Remove<PawnSpatialUpdateSystem>(ref loop);
            PlayerLoop.SetPlayerLoop(loop);
        }

        static void RegisterInternal(Pawn pawn)
        {
            var key = GetCell(pawn.transform.position);
            AddToCell(pawn, key);
            _pawnCell[pawn.Hash] = key;
            _pawns[pawn.Hash] = pawn;
            _pawnListIndex[pawn.Hash] = _pawnList.Count;
            _pawnList.Add(pawn);
        }
        static void UnregisterInternal(Pawn pawn)
        {
            if (!_pawnCell.TryGetValue(pawn.Hash, out var key)) return;
            RemoveFromCell(pawn, key);
            _pawnCell.Remove(pawn.Hash);
            _pawns.Remove(pawn.Hash);
            
            int idx  = _pawnListIndex[pawn.Hash];
            int last = _pawnList.Count - 1;
            if (idx != last)
            {
                var swapped = _pawnList[last];
                _pawnList[idx] = swapped;
                _pawnListIndex[swapped.Hash] = idx;
            }
            _pawnList.RemoveAt(last);
            _pawnListIndex.Remove(pawn.Hash);
        }
        static void UpdatePositionInternal(Pawn pawn)
        {
            var newKey = GetCell(pawn.transform.position);
            if (!_pawnCell.TryGetValue(pawn.Hash, out var oldKey)) return;
            if (oldKey == newKey) return;
            RemoveFromCell(pawn, oldKey);
            AddToCell(pawn, newKey);
            _pawnCell[pawn.Hash] = newKey;
        }
        
        static void Tick()
        {
            for (int i = 0; i < _pawnList.Count; i++)
                UpdatePositionInternal(_pawnList[i]);
        }
        
        public static void GetInRadius(Vector3 center, float radius, List<Pawn> results)
            => GetInRadiusInternal(center, radius, results, null);
        public static void GetInRadius(Vector3 center, float radius, List<Pawn> results, Func<Pawn, bool> cond)
            => GetInRadiusInternal(center, radius, results, cond);
        public static bool TryGetNearest(Vector3 center, float radius, out Pawn result)
            => TryGetNearestInternal(center, radius, null, out result);
        public static bool TryGetNearest(Vector3 center, float radius, Func<Pawn, bool> cond, out Pawn result)
            => TryGetNearestInternal(center, radius, cond, out result);
        public static bool HasAnyInRadius(Vector3 center, float radius)
            => HasAnyInRadiusInternal(center, radius, null);
        public static bool HasAnyInRadius(Vector3 center, float radius, Func<Pawn, bool> cond)
            => HasAnyInRadiusInternal(center, radius, cond);

        static void GetInRadiusInternal(Vector3 center, float radius, List<Pawn> results, Func<Pawn, bool> cond)
        {
            results.Clear();
            float radiusSq = radius * radius;
            int minX = Mathf.FloorToInt((center.x - radius) * _invCellSize);
            int maxX = Mathf.FloorToInt((center.x + radius) * _invCellSize);
            int minZ = Mathf.FloorToInt((center.z - radius) * _invCellSize);
            int maxZ = Mathf.FloorToInt((center.z + radius) * _invCellSize);

            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!_cells.TryGetValue(new CellKey(x, z), out var cell)) continue;
                    for (int i = 0; i < cell.Count; i++)
                    {
                        var pawn = cell[i];
                        if (pawn == null || !pawn.IsActive) continue;
                        if (Vector3.SqrMagnitude(pawn.transform.position - center) > radiusSq) continue;
                        if (cond != null && !cond(pawn)) continue;
                        results.Add(pawn);
                    }
                }
        }
        static bool TryGetNearestInternal(Vector3 center, float radius, Func<Pawn, bool> cond, out Pawn result)
        {
            Pawn nearest = null;
            float bestSq = radius * radius;

            int minX = Mathf.FloorToInt((center.x - radius) * _invCellSize);
            int maxX = Mathf.FloorToInt((center.x + radius) * _invCellSize);
            int minZ = Mathf.FloorToInt((center.z - radius) * _invCellSize);
            int maxZ = Mathf.FloorToInt((center.z + radius) * _invCellSize);

            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!_cells.TryGetValue(new CellKey(x, z), out var cell)) continue;
                    for (int i = 0; i < cell.Count; i++)
                    {
                        var pawn = cell[i];
                        if (pawn == null || !pawn.IsActive) continue;
                        float sq = Vector3.SqrMagnitude(pawn.transform.position - center);
                        if (sq > bestSq) continue;
                        if (cond != null && !cond(pawn)) continue;
                        bestSq  = sq;
                        nearest = pawn;
                    }
                }

            result = nearest;
            return result != null;
        }
        static bool HasAnyInRadiusInternal(Vector3 center, float radius, Func<Pawn, bool> cond)
        {
            float radiusSq = radius * radius;

            int minX = Mathf.FloorToInt((center.x - radius) * _invCellSize);
            int maxX = Mathf.FloorToInt((center.x + radius) * _invCellSize);
            int minZ = Mathf.FloorToInt((center.z - radius) * _invCellSize);
            int maxZ = Mathf.FloorToInt((center.z + radius) * _invCellSize);

            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!_cells.TryGetValue(new CellKey(x, z), out var cell)) continue;
                    for (int i = 0; i < cell.Count; i++)
                    {
                        var pawn = cell[i];
                        if (pawn == null || !pawn.IsActive) continue;
                        if (Vector3.SqrMagnitude(pawn.transform.position - center) > radiusSq) continue;
                        if (cond != null && !cond(pawn)) continue;
                        return true;
                    }
                }

            return false;
        }

        static CellKey GetCell(Vector3 pos)
            => new CellKey(
                Mathf.FloorToInt(pos.x * _invCellSize),
                Mathf.FloorToInt(pos.z * _invCellSize)
            );

        static void AddToCell(Pawn pawn, CellKey key)
        {
            if (!_cells.TryGetValue(key, out var list))
            {
                list = new List<Pawn>(8);
                _cells[key] = list;
            }
            list.Add(pawn);
        }
        static void RemoveFromCell(Pawn pawn, CellKey key)
        {
            if (!_cells.TryGetValue(key, out var list)) return;
            int idx = list.IndexOf(pawn);
            if (idx < 0) return;
            int last = list.Count - 1;
            if (idx != last)
                list[idx] = list[last];
            list.RemoveAt(last);
            if (list.Count == 0)
                _cells.Remove(key);
        }

        readonly struct CellKey : IEquatable<CellKey>
        {
            readonly int _x;
            readonly int _z;

            public CellKey(int x, int z) { _x = x; _z = z; }

            public bool Equals(CellKey other) => _x == other._x && _z == other._z;
            public override bool Equals(object obj) => obj is CellKey other && Equals(other);

            public override int GetHashCode()
            {
                uint h = 2166136261u;
                h = (h ^ unchecked((uint)_x)) * 16777619u;
                h = (h ^ unchecked((uint)_z)) * 16777619u;
                return unchecked((int)h);
            }

            public static bool operator ==(CellKey a, CellKey b) => a.Equals(b);
            public static bool operator !=(CellKey a, CellKey b) => !a.Equals(b);
        }
    }
}