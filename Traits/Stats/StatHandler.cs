using System;
using System.Collections.Generic;

using ModifiableVariable;
using ModifiableVariable.Entities;

using SackranyPawn.Entities.Modules;
using SackranyPawn.Traits.Stats.Static;

using UnityEngine;

namespace SackranyPawn.Traits.Stats
{
    [Serializable]
    [UpdateOrder(Order.BeforeAll)]
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
            StatAdded = null;
            StatRemoved = null;
        }

        public bool Register<T>(float baseValue) where T : IStat
            => RegisterInternal(StatRegistry.GetId<T>(), baseValue);
        public bool Register(IStat stat, float baseValue)
            => RegisterInternal(stat.Id, baseValue);
        bool RegisterInternal(int id, float baseValue)
        {
            if (_stats.ContainsKey(id)) return false;
            _stats[id] = new Modifiable<float>(baseValue);
            StatAdded?.Invoke(id);
            return true;
        }
        public bool Unregister<T>() where T : IStat
        {
            int id = StatRegistry.GetId<T>();
            if (!_stats.TryGetValue(id, out var ev)) return false;
            ev.Clear();
            _stats.Remove(id);
            StatRemoved?.Invoke(id);
            return true;
        }
        public bool Unregister(IStat stat)
        {
            int id = stat.Id;
            if (!_stats.TryGetValue(id, out var ev)) return false;
            ev.Clear();
            _stats.Remove(id);
            StatRemoved?.Invoke(id);
            return true;
        }

        public Modifiable<float> GetStat<T>() where T : IStat
        {
            _stats.TryGetValue(StatRegistry.GetId<T>(), out var v);
            return v;
        }
        public Modifiable<float> GetStat(IStat stat)
        {
            _stats.TryGetValue(stat.Id, out var v);
            return v;
        }
        
        public bool TryGetStat<T>(out Modifiable<float> value) where T : IStat
            => _stats.TryGetValue(StatRegistry.GetId<T>(), out value);
        public bool TryGetStat(IStat stat, out Modifiable<float> value)
            => _stats.TryGetValue(stat.Id, out value);
        
        public bool HasStat<T>() where T : IStat
            => _stats.ContainsKey(StatRegistry.GetId<T>());
        public bool HasStat(IStat stat) 
            => _stats.ContainsKey(stat.Id);
        public float GetValue<T>(float fallback = 0f) where T : IStat
            => TryGetStat<T>(out var v) ? v.Value : fallback;
        public float GetValue(IStat stat, float fallback = 0f)
            => TryGetStat(stat, out var v) ? v.Value : fallback;

        public ValueChangedHandler<float> OnStatChanged<T>(ValueChangedDelegate<float> callback) where T : IStat 
            => !TryGetStat<T>(out var stat) ? default : stat.OnValueChanged(callback);
        public ValueChangedHandler<float> OnStatChanged(IStat stat, ValueChangedDelegate<float> callback) 
            => !TryGetStat(stat, out var s) ? default : s.OnValueChanged(callback);
        
        public event Action<int> StatAdded;
        public event Action<int> StatRemoved;
    }
}