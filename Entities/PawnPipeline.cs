using System;
using System.Collections.Generic;

using SackranyPawn.Cache;
using SackranyPawn.Components;
using SackranyPawn.Entities;
using SackranyPawn.Entities.Modules;
using SackranyPawn.Managers;
using SackranyPawn.Traits.PawnEvents;
using SackranyPawn.Traits.PawnTags;
using SackranyPawn.Traits.Stats;

using UnityEngine;

namespace SackranyPawn.Entities
{
    public class PawnPipeline
    {
        readonly List<Pawn> _working = new(64);
        readonly List<Action<List<Pawn>>> _ops = new();

        PawnPipeline() { }

        /// <summary>Seed the pipeline with all registered pawns.</summary>
        public static PawnPipeline All()
        {
            var p = new PawnPipeline();
            p._ops.Add(set =>
            {
                set.Clear();
                set.AddRange(PawnRegister.RegisteredPawns);
            });
            return p;
        }
        
        /// <summary>Keep only pawns that have the given tag.</summary>
        public PawnPipeline WithTag<T>() where T : IPawnTag
        {
            int id = TypeRegistry<IPawnTag>.Id<T>.Value;
            _ops.Add(set => set.RemoveAll(p => !p.Tag.HasTag(id)));
            return this;
        }

        /// <summary>Keep only pawns that do not have the given tag.</summary>
        public PawnPipeline WithoutTag<T>() where T : IPawnTag
        {
            int id = TypeRegistry<IPawnTag>.Id<T>.Value;
            _ops.Add(set => set.RemoveAll(p => p.Tag.HasTag(id)));
            return this;
        }

        /// <summary>Keep only pawns that have the given limb type.</summary>
        public PawnPipeline Has<T>() where T : Limb
        {
            int id = LimbRegistry.GetId<T>();
            _ops.Add(set => set.RemoveAll(p => !p.Has(LimbRegistry.GetTypeById(id))));
            return this;
        }

        /// <summary>Keep only pawns that do not have the given limb type.</summary>
        public PawnPipeline HasNot<T>() where T : Limb
        {
            int id = LimbRegistry.GetId<T>();
            _ops.Add(set => set.RemoveAll(p => p.Has(LimbRegistry.GetTypeById(id))));
            return this;
        }

        /// <summary>Keep only pawns on the given team.</summary>
        public PawnPipeline WithTeam(TeamInfo team)
        {
            _ops.Add(set => set.RemoveAll(p => p.Team != team));
            return this;
        }

        /// <summary>Keep only pawns not on the given team.</summary>
        public PawnPipeline NotOnTeam(TeamInfo team)
        {
            _ops.Add(set => set.RemoveAll(p => p.Team == team));
            return this;
        }

        /// <summary>Keep only pawns with the given archetype.</summary>
        public PawnPipeline WithArchetype(PawnArchetype archetype)
        {
            _ops.Add(set => set.RemoveAll(p => p.Archetype != archetype));
            return this;
        }

        /// <summary>Keep only pawns that are currently active.</summary>
        public PawnPipeline Active()
        {
            _ops.Add(set => set.RemoveAll(p => !p.IsActive));
            return this;
        }

        /// <summary>Keep only pawns matching the given predicate.</summary>
        public PawnPipeline Where(Func<Pawn, bool> predicate)
        {
            _ops.Add(set => set.RemoveAll(p => !predicate(p)));
            return this;
        }

        /// <summary>Keep only pawns within the given world-space radius.</summary>
        public PawnPipeline WithinRadius(Vector3 center, float radius)
        {
            float radiusSq = radius * radius;
            _ops.Add(set => set.RemoveAll(p =>
                (p.transform.position - center).sqrMagnitude > radiusSq));
            return this;
        }

        /// <summary>Keep only pawns whose stat value is within [min, max].</summary>
        public PawnPipeline WithStat<T>(float min, float max) where T : IStat
        {
            _ops.Add(set => set.RemoveAll(p =>
            {
                float v = p.GetStatValue<T>();
                return v < min || v > max;
            }));
            return this;
        }

        /// <summary>Keep only pawns whose stat value is above the given minimum.</summary>
        public PawnPipeline WithStatAbove<T>(float min) where T : IStat
        {
            _ops.Add(set => set.RemoveAll(p => p.GetStatValue<T>() < min));
            return this;
        }

        /// <summary>Keep only pawns whose stat value is below the given maximum.</summary>
        public PawnPipeline WithStatBelow<T>(float max) where T : IStat
        {
            _ops.Add(set => set.RemoveAll(p => p.GetStatValue<T>() > max));
            return this;
        }

        /// <summary>Add specific pawns to the current set, regardless of previous filters.</summary>
        public PawnPipeline Include(params Pawn[] pawns)
        {
            _ops.Add(set =>
            {
                foreach (var p in pawns)
                    if (p != null && !set.Contains(p)) set.Add(p);
            });
            return this;
        }

        /// <summary>Remove specific pawns from the current set, regardless of previous filters.</summary>
        public PawnPipeline Exclude(params Pawn[] pawns)
        {
            _ops.Add(set =>
            {
                foreach (var p in pawns)
                    set.Remove(p);
            });
            return this;
        }

        /// <summary>Sort the current set by distance to the given point, nearest first by default.</summary>
        public PawnPipeline SortByDistance(Vector3 point, bool descending = false)
        {
            _ops.Add(set => set.Sort((a, b) =>
            {
                int cmp = (a.transform.position - point).sqrMagnitude
                    .CompareTo((b.transform.position - point).sqrMagnitude);
                return descending ? -cmp : cmp;
            }));
            return this;
        }

        /// <summary>Sort the current set by the given stat value, ascending or descending.</summary>
        public PawnPipeline SortByStat<T>(bool descending = false) where T : IStat
        {
            _ops.Add(set => set.Sort((a, b) =>
            {
                int cmp = a.GetStatValue<T>().CompareTo(b.GetStatValue<T>());
                return descending ? -cmp : cmp;
            }));
            return this;
        }

        /// <summary>Sort the current set using a custom comparison delegate.</summary>
        public PawnPipeline SortBy(Comparison<Pawn> comparison)
        {
            _ops.Add(set => set.Sort(comparison));
            return this;
        }

        /// <summary>Keep only the first N pawns from the current set.</summary>
        public PawnPipeline Take(int count)
        {
            _ops.Add(set =>
            {
                if (set.Count > count) set.RemoveRange(count, set.Count - count);
            });
            return this;
        }

        /// <summary>Skip the first N pawns from the current set.</summary>
        public PawnPipeline Skip(int count)
        {
            _ops.Add(set =>
            {
                if (count > 0) set.RemoveRange(0, Mathf.Min(count, set.Count));
            });
            return this;
        }

        /// <summary>Randomly shuffle the current set.</summary>
        public PawnPipeline Shuffle()
        {
            _ops.Add(set =>
            {
                for (int i = set.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (set[i], set[j]) = (set[j], set[i]);
                }
            });
            return this;
        }

        List<Pawn> Evaluate()
        {
            _working.Clear();
            for (int i = 0; i < _ops.Count; i++)
                _ops[i](_working);
            return _working;
        }

        /// <summary>Execute an action for each pawn in the result set.</summary>
        public void ForEach(Action<Pawn> action)
        {
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++) action(set[i]);
        }

        /// <summary>Execute an action for each pawn that has limb T.</summary>
        public void ForEach<T>(Action<T> action) where T : Limb
        {
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++)
                if (set[i].TryGet(out T limb)) action(limb);
        }

        /// <summary>Execute an action with both pawn and limb T for each matching pawn.</summary>
        public void ForEach<T>(Action<Pawn, T> action) where T : Limb
        {
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++)
                if (set[i].TryGet(out T limb)) action(set[i], limb);
        }

        /// <summary>Execute an action with both limbs TA and TB for each pawn that has both.</summary>
        public void ForEach<TA, TB>(Action<TA, TB> action) where TA : Limb where TB : Limb
        {
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++)
                if (set[i].TryGet(out TA a) && set[i].TryGet(out TB b)) action(a, b);
        }

        /// <summary>Publish an event on every pawn in the result set.</summary>
        public void Broadcast<E>() where E : IEvent
        {
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++) set[i].Event.Publish<E>();
        }

        /// <summary>Publish an event with data on every pawn in the result set.</summary>
        public void Broadcast<E, T>(T data) where E : IEvent
        {
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++) set[i].Event.Publish<E, T>(data);
        }

        /// <summary>Return the first pawn in the result set.</summary>
        public bool First(out Pawn result)
        {
            var set = Evaluate();
            result = set.Count > 0 ? set[0] : null;
            return result != null;
        }

        /// <summary>Return the first pawn that has limb T.</summary>
        public bool First<T>(out Pawn pawn, out T limb) where T : Limb
        {
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++)
            {
                if (!set[i].TryGet(out limb)) continue;
                pawn = set[i];
                return true;
            }
            pawn = null;
            limb = null;
            return false;
        }

        /// <summary>Return the pawn nearest to the given point.</summary>
        public bool Nearest(Vector3 point, out Pawn result)
        {
            result = null;
            float bestSq = float.MaxValue;
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++)
            {
                float sq = (set[i].transform.position - point).sqrMagnitude;
                if (sq >= bestSq) continue;
                bestSq = sq;
                result = set[i];
            }
            return result != null;
        }

        /// <summary>Return the pawn farthest from the given point.</summary>
        public bool Farthest(Vector3 point, out Pawn result)
        {
            result = null;
            float worstSq = float.MinValue;
            var set = Evaluate();
            for (int i = 0; i < set.Count; i++)
            {
                float sq = (set[i].transform.position - point).sqrMagnitude;
                if (sq <= worstSq) continue;
                worstSq = sq;
                result = set[i];
            }
            return result != null;
        }

        /// <summary>Return a random pawn from the result set.</summary>
        public bool Random(out Pawn result)
        {
            var set = Evaluate();
            if (set.Count == 0) { result = null; return false; }
            result = set[UnityEngine.Random.Range(0, set.Count)];
            return true;
        }

        /// <summary>Return true if the result set contains at least one pawn.</summary>
        public bool Any() => Evaluate().Count > 0;

        /// <summary>Return the number of pawns in the result set.</summary>
        public int Count() => Evaluate().Count;

        /// <summary>Materialize the result set into the given list.</summary>
        public int ToList(List<Pawn> results)
        {
            results.Clear();
            results.AddRange(Evaluate());
            return results.Count;
        }
    }
}