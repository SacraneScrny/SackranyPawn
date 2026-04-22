using System;
using System.Collections.Generic;

using ModifiableVariable;

using SackranyPawn.Entities.Modules;
using SackranyPawn.Traits.Stats.Static;

using UnityEngine;

namespace SackranyPawn.Traits.Stats
{
    [UpdateOrder(Order.BeforeAll)]
    [Serializable]
    public class StatHandler : Limb
    {
        [SerializeField][SerializeReference][SubclassSelector]
        public AStat[] Default;

        readonly Dictionary<int, Modifiable<float>> _stats = new();

        protected override void OnStart()
        {
            foreach (var stat in Default)
                RegisterInternal(stat.Id, stat.baseValue);
        }
        protected override void OnReset()
        {
            ClearAll();
            foreach (var stat in Default)
                RegisterInternal(stat.Id, stat.baseValue);
        }
        protected override void OnDispose() => ClearAll();

        void ClearAll()
        {
            foreach (var v in _stats.Values) v.Clear();
            _stats.Clear();
            OnStatAdded = null;
            OnStatRemoved = null;
        }

        public bool Register<T>(float baseValue) where T : IStat
            => RegisterInternal(StatRegistry.GetId<T>(), baseValue);
        public bool Register(IStat stat, float baseValue)
            => RegisterInternal(stat.Id, baseValue);
        bool RegisterInternal(int id, float baseValue)
        {
            if (_stats.ContainsKey(id)) return false;
            _stats[id] = new Modifiable<float>(baseValue);
            OnStatAdded?.Invoke(id);
            return true;
        }
        public bool Unregister<T>() where T : IStat
        {
            int id = StatRegistry.GetId<T>();
            if (!_stats.TryGetValue(id, out var ev)) return false;
            ev.Clear();
            _stats.Remove(id);
            OnStatRemoved?.Invoke(id);
            return true;
        }

        public Modifiable<float> GetStat<T>() where T : IStat
        {
            _stats.TryGetValue(StatRegistry.GetId<T>(), out var v);
            return v;
        }
        public bool TryGetStat<T>(out Modifiable<float> value) where T : IStat
            => _stats.TryGetValue(StatRegistry.GetId<T>(), out value);
        public bool HasStat<T>() where T : IStat
            => _stats.ContainsKey(StatRegistry.GetId<T>());
        public float GetValue<T>(float fallback = 0f) where T : IStat
            => TryGetStat<T>(out var v) ? v.GetValue() : fallback;

        public event Action<int> OnStatAdded;
        public event Action<int> OnStatRemoved;
    }
}