using System.Collections.Generic;

using SackranyPawn.Components;
using SackranyPawn.Entities;

using UnityEngine;

namespace SackranyPawn.Managers
{
    public static class PawnPool
    {
        public static int GetCount(PawnArchetype archetype) =>
            _pawnPool.TryGetValue(archetype, out var pawns) ? pawns.Count : 0;
        
        static readonly Dictionary<PawnArchetype, Stack<Pawn>> _pawnPool = new ();
        static Dictionary<PawnArchetype, Pawn> _templates = new();
        static Dictionary<int, Pawn> _goToPawn = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _pawnPool.Clear();
            _templates.Clear();
            _goToPawn.Clear();
        }
        
        public static void PreWarm(Pawn template, int count)
        {
            RegisterTemplate(template);

            if (!_pawnPool.TryGetValue(template.Archetype, out var pawns))
            {
                pawns = new Stack<Pawn>(count);
                _pawnPool.Add(template.Archetype, pawns);
            }

            for (int i = 0; i < count; i++)
            {
                var p = CreatePawn(template.Archetype);
                if (p == null) continue;
                p.OnPushed();
                pawns.Push(p);
            }
        }

        #region POP
        public static Pawn Pop(Pawn pawn)
        {
            if (pawn == null) return null;
            
            RegisterTemplate(pawn);
            
            if (!_pawnPool.TryGetValue(pawn.Archetype, out var pawns))
            {
                pawns = new Stack<Pawn>();
                _pawnPool.Add(pawn.Archetype, pawns);
            }
            return PopInternal(pawn.Archetype, pawns);
        }
        public static Pawn Pop(GameObject pawnGameObject)
        {
            if (pawnGameObject == null) return null;
            var pawn = ResolvePawn(pawnGameObject);
            if (pawn == null) return null;
            return Pop(pawn);
        }
        
        static Pawn PopInternal(PawnArchetype archetype, Stack<Pawn> pawns)
        {
            var p = pawns.Count == 0 ? CreatePawn(archetype) : pawns.Pop();
            p?.OnPopped();
            return p;
        }
        #endregion

        #region PUSH
        public static void Push(Pawn pawn)
        {
            if (pawn == null) return;
            if (!_pawnPool.TryGetValue(pawn.Archetype, out var pawns))
            {
                pawns = new Stack<Pawn>();
                _pawnPool.Add(pawn.Archetype, pawns);
            }
            pawns.Push(pawn);
            pawn.OnPushed();
        }
        public static void Push(GameObject pawnGameObject)
        {
            if (pawnGameObject == null) return;
            var pawn = ResolvePawn(pawnGameObject);
            if (pawn == null) return;
            
            Push(pawn);
        }
        #endregion
        
        #region CREATE
        static Pawn CreatePawn(PawnArchetype archetype)
        {
            if (!_templates.TryGetValue(archetype, out var template)) return null;
            var g = Object.Instantiate(template.gameObject);
            var p = g.GetComponent<Pawn>();
            _goToPawn[g.GetInstanceID()] = p;
            return p;
        }
        #endregion

        #region CLEAR
        public static void Clear(PawnArchetype archetype)
        {
            if (!_pawnPool.TryGetValue(archetype, out var pawns)) return;
            while (pawns.Count > 0)
            {
                var p = pawns.Pop();
                if (p != null) Object.Destroy(p.gameObject);
            }
            _templates.Remove(archetype);
        }
        public static void ClearAll()
        {
            foreach (var (archetype, _) in _pawnPool) Clear(archetype);
            _pawnPool.Clear();
            _goToPawn.Clear();
        }
        #endregion
        
        #region HELPERS
        static void RegisterTemplate(Pawn pawn)
        {
            if (!_templates.TryAdd(pawn.Archetype, pawn)) return;
            _goToPawn[pawn.gameObject.GetInstanceID()] = pawn;
        }
        static Pawn ResolvePawn(GameObject go)
        {
            int id = go.GetInstanceID();
            if (_goToPawn.TryGetValue(id, out var cached)) return cached;

            var pawn = go.GetComponent<Pawn>();
            if (pawn != null) _goToPawn[id] = pawn;
            return pawn;
        }
        #endregion
    }
}