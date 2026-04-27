using System;
using System.Collections.Generic;

using SackranyPawn.Components;

using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace SackranyPawn.Managers
{
    public enum SpatialAxes { XZ, XY, YZ }

    public static class PawnSpatial
    {
        struct PawnSpatialUpdateSystem { }

        public static float CellSize { get; private set; } = 10f;
        public static SpatialAxes Axes { get; private set; } = SpatialAxes.XZ;

        public static float MovementThreshold
        {
            get => _movementThreshold;
            set
            {
                _movementThreshold = value;
                _movementThresholdSq = value * value;
            }
        }

        static float _movementThreshold;
        static float _movementThresholdSq;
        static float _invCellSize;

        static readonly Dictionary<CellKey, List<Pawn>> _cells = new();
        static readonly Dictionary<int, CellKey> _pawnCell = new();
        static readonly Dictionary<int, Vector3> _lastPositions = new();
        static readonly List<Pawn> _pawnList = new();
        static readonly Dictionary<int, int> _pawnListIndex = new();

        static bool _isUpdating;
        static readonly List<(Pawn pawn, bool add)> _pendingChanges = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            _cells.Clear();
            _pawnList.Clear();
            _pawnListIndex.Clear();
            _pawnCell.Clear();
            _lastPositions.Clear();
            _isUpdating = false;
            _pendingChanges.Clear();

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
            PawnRegister.OnPawnRegistered -= RegisterInternal;
            PawnRegister.OnPawnUnregistered -= UnregisterInternal;

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopUtils.Remove<PawnSpatialUpdateSystem>(ref loop);
            PlayerLoop.SetPlayerLoop(loop);
        }

        public static void Configure(
            float cellSize,
            SpatialAxes axes = SpatialAxes.XZ,
            float movementThreshold = 0f)
        {
            bool needsRebuild = !Mathf.Approximately(cellSize, CellSize) || axes != Axes;

            CellSize = cellSize;
            Axes = axes;
            _invCellSize = 1f / cellSize;
            MovementThreshold = movementThreshold;

            if (needsRebuild) Rebuild();
        }

        static void Rebuild()
        {
            _cells.Clear();
            _pawnCell.Clear();
            _lastPositions.Clear();

            for (int i = 0; i < _pawnList.Count; i++)
            {
                var pawn = _pawnList[i];
                var pos = pawn.transform.position;
                var key = GetCell(pos);
                AddToCell(pawn, key);
                _pawnCell[pawn.Hash] = key;
                if (_movementThresholdSq > 0f)
                    _lastPositions[pawn.Hash] = pos;
            }
        }

        #region REGISTER / UNREGISTER
        static void RegisterInternal(Pawn pawn)
        {
            if (_isUpdating) { _pendingChanges.Add((pawn, true)); return; }
            RegisterInternalImmediate(pawn);
        }
        static void UnregisterInternal(Pawn pawn)
        {
            if (_isUpdating) { _pendingChanges.Add((pawn, false)); return; }
            UnregisterInternalImmediate(pawn);
        }

        static void RegisterInternalImmediate(Pawn pawn)
        {
            var pos = pawn.transform.position;
            var key = GetCell(pos);
            AddToCell(pawn, key);
            _pawnCell[pawn.Hash] = key;
            _pawnListIndex[pawn.Hash] = _pawnList.Count;
            _pawnList.Add(pawn);
            if (_movementThresholdSq > 0f)
                _lastPositions[pawn.Hash] = pos;
        }
        static void UnregisterInternalImmediate(Pawn pawn)
        {
            if (!_pawnCell.TryGetValue(pawn.Hash, out var key)) return;
            RemoveFromCell(pawn, key);
            _pawnCell.Remove(pawn.Hash);
            _lastPositions.Remove(pawn.Hash);

            int idx = _pawnListIndex[pawn.Hash];
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
        #endregion

        #region TICK
        static void Tick()
        {
            _isUpdating = true;
            for (int i = 0; i < _pawnList.Count; i++)
                UpdatePositionInternal(_pawnList[i]);
            _isUpdating = false;
            FlushPending();
        }

        static void UpdatePositionInternal(Pawn pawn)
        {
            var pos = pawn.transform.position;

            if (_movementThresholdSq > 0f)
            {
                if (_lastPositions.TryGetValue(pawn.Hash, out var last))
                    if (SqrDist2D(pos, last) < _movementThresholdSq) return;
                _lastPositions[pawn.Hash] = pos;
            }

            var newKey = GetCell(pos);
            if (!_pawnCell.TryGetValue(pawn.Hash, out var oldKey)) return;
            if (oldKey == newKey) return;
            RemoveFromCell(pawn, oldKey);
            AddToCell(pawn, newKey);
            _pawnCell[pawn.Hash] = newKey;
        }

        static void FlushPending()
        {
            if (_pendingChanges.Count == 0) return;
            for (int i = 0; i < _pendingChanges.Count; i++)
            {
                var (pawn, add) = _pendingChanges[i];
                if (add) RegisterInternalImmediate(pawn);
                else UnregisterInternalImmediate(pawn);
            }
            _pendingChanges.Clear();
        }
        #endregion

        #region QUERIES
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
            GetRange(center, radius, out int minA, out int maxA, out int minB, out int maxB);

            for (int a = minA; a <= maxA; a++)
                for (int b = minB; b <= maxB; b++)
                {
                    if (!_cells.TryGetValue(new CellKey(a, b), out var cell)) continue;
                    for (int i = 0; i < cell.Count; i++)
                    {
                        var pawn = cell[i];
                        if (pawn == null || !pawn.IsActive) continue;
                        if (SqrDist2D(pawn.transform.position, center) > radiusSq) continue;
                        if (cond != null && !cond(pawn)) continue;
                        results.Add(pawn);
                    }
                }
        }
        static bool TryGetNearestInternal(Vector3 center, float radius, Func<Pawn, bool> cond, out Pawn result)
        {
            Pawn nearest = null;
            float bestSq = radius * radius;
            GetRange(center, radius, out int minA, out int maxA, out int minB, out int maxB);

            for (int a = minA; a <= maxA; a++)
                for (int b = minB; b <= maxB; b++)
                {
                    if (!_cells.TryGetValue(new CellKey(a, b), out var cell)) continue;
                    for (int i = 0; i < cell.Count; i++)
                    {
                        var pawn = cell[i];
                        if (pawn == null || !pawn.IsActive) continue;
                        float sq = SqrDist2D(pawn.transform.position, center);
                        if (sq > bestSq) continue;
                        if (cond != null && !cond(pawn)) continue;
                        bestSq = sq;
                        nearest = pawn;
                    }
                }

            result = nearest;
            return result != null;
        }
        static bool HasAnyInRadiusInternal(Vector3 center, float radius, Func<Pawn, bool> cond)
        {
            float radiusSq = radius * radius;
            GetRange(center, radius, out int minA, out int maxA, out int minB, out int maxB);

            for (int a = minA; a <= maxA; a++)
                for (int b = minB; b <= maxB; b++)
                {
                    if (!_cells.TryGetValue(new CellKey(a, b), out var cell)) continue;
                    for (int i = 0; i < cell.Count; i++)
                    {
                        var pawn = cell[i];
                        if (pawn == null || !pawn.IsActive) continue;
                        if (SqrDist2D(pawn.transform.position, center) > radiusSq) continue;
                        if (cond != null && !cond(pawn)) continue;
                        return true;
                    }
                }
            return false;
        }
        #endregion

        #region MATH HELPERS
        static void GetAxes(Vector3 pos, out float a, out float b)
        {
            switch (Axes)
            {
                case SpatialAxes.XY: a = pos.x; b = pos.y; return;
                case SpatialAxes.YZ: a = pos.y; b = pos.z; return;
                default: a = pos.x; b = pos.z; return;
            }
        }
        static float SqrDist2D(Vector3 a, Vector3 b)
        {
            switch (Axes)
            {
                case SpatialAxes.XY: { float dx = a.x-b.x, dy = a.y-b.y; return dx*dx + dy*dy; }
                case SpatialAxes.YZ: { float dy = a.y-b.y, dz = a.z-b.z; return dy*dy + dz*dz; }
                default: { float dx = a.x-b.x, dz = a.z-b.z; return dx*dx + dz*dz; }
            }
        }

        static CellKey GetCell(Vector3 pos)
        {
            GetAxes(pos, out float a, out float b);
            return new CellKey(
                Mathf.FloorToInt(a * _invCellSize),
                Mathf.FloorToInt(b * _invCellSize)
            );
        }

        static void GetRange(Vector3 center, float radius,
            out int minA, out int maxA, out int minB, out int maxB)
        {
            GetAxes(center, out float a, out float b);
            minA = Mathf.FloorToInt((a - radius) * _invCellSize);
            maxA = Mathf.FloorToInt((a + radius) * _invCellSize);
            minB = Mathf.FloorToInt((b - radius) * _invCellSize);
            maxB = Mathf.FloorToInt((b + radius) * _invCellSize);
        }
        #endregion

        #region CELLS
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
        #endregion

        readonly struct CellKey : IEquatable<CellKey>
        {
            readonly int _a;
            readonly int _b;

            public CellKey(int a, int b) { _a = a; _b = b; }

            public bool Equals(CellKey other) => _a == other._a && _b == other._b;
            public override bool Equals(object obj) => obj is CellKey other && Equals(other);

            public override int GetHashCode()
            {
                uint h = 2166136261u;
                h = (h ^ unchecked((uint)_a)) * 16777619u;
                h = (h ^ unchecked((uint)_b)) * 16777619u;
                return unchecked((int)h);
            }

            public static bool operator ==(CellKey a, CellKey b) => a.Equals(b);
            public static bool operator !=(CellKey a, CellKey b) => !a.Equals(b);
        }
    }
}