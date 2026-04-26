using System;
using System.Collections.Generic;

using ModifiableVariable;
using ModifiableVariable.Entities;
using ModifiableVariable.Stages.StageFactory;

using SackranyPawn.Entities.Modules;
using SackranyPawn.Traits.Conditions.Static;

using UnityEngine;

namespace SackranyPawn.Traits.Conditions
{
    [Serializable]
    [UpdateOrder(Order.BeforeAll)]
    public class ConditionHandler : Limb
    {
        [SerializeField][SerializeReference][SubclassSelector]
        public ACondition[] Default;

        readonly Dictionary<int, int> _blocks = new();
        readonly Dictionary<int, GateModifiable<bool>> _gates = new();

        protected override void OnStart()
        {
            foreach (var condition in Default)
                BlockInternal(condition.Id, 1);
        }
        protected override void OnReset()
        {
            _blocks.Clear();
            OnBlocked = null;
            OnUnblocked = null;
            foreach (var condition in Default)
                BlockInternal(condition.Id, 1);
            foreach (var b in _gates.Values) b.Clear();
            _gates.Clear();
        }
        protected override void OnDispose()
        {
            _blocks.Clear();
            foreach (var b in _gates.Values) b.Clear();
            _gates.Clear();
        }
        
        public bool Block<T>(int amount = 1) where T : ICondition
            => BlockInternal(ConditionRegistry.GetId<T>(), amount);
        public bool Block(ICondition condition, int amount = 1)
            => BlockInternal(condition.Id, amount);
        bool BlockInternal(int id, int amount)
        {
            if (amount <= 0) return false;
            
            bool before = IsAllowedInternal(id);
            _blocks.TryGetValue(id, out int current);
            _blocks[id] = current + amount;
            NotifyIfChanged(id, before);
            return true;
        }

        public bool Unblock<T>(int amount = 1) where T : ICondition
            => UnblockInternal(ConditionRegistry.GetId<T>(), amount);
        public bool Unblock(ICondition condition, int amount = 1)
            => UnblockInternal(condition.Id, amount);
        public bool UnblockAll<T>() where T : ICondition
        {
            int id = ConditionRegistry.GetId<T>();
            if (!_blocks.ContainsKey(id)) return false;
            bool before = IsAllowedInternal(id);
            _blocks.Remove(id);
            NotifyIfChanged(id, before);
            return true;
        }
        public bool UnblockAll(ICondition condition)
        {
            int id = condition.Id;
            if (!_blocks.ContainsKey(id)) return false;
            bool before = IsAllowedInternal(id);
            _blocks.Remove(id);
            NotifyIfChanged(id, before);
            return true;
        }
        bool UnblockInternal(int id, int amount)
        {
            if (amount <= 0) return false;
            
            if (!_blocks.TryGetValue(id, out int current)) return false;
            bool before = IsAllowedInternal(id);
            int next = Math.Max(0, current - amount);
            if (next == 0) _blocks.Remove(id);
            else _blocks[id] = next;
            NotifyIfChanged(id, before);
            return true;
        }
        
        public bool IsAllowed<T>() where T : ICondition => IsAllowedInternal(ConditionRegistry.GetId<T>());
        public bool IsAllowed(ICondition condition) => IsAllowedInternal(condition.Id);
        bool IsAllowedInternal(int id)
        {
            if (_blocks.TryGetValue(id, out int count) && count > 0) return false;
            if (_gates.TryGetValue(id, out var b) && b.Count > 0 && !b.Value) return false;
            return true;
        }
        
        public bool IsBlocked<T>() where T : ICondition
            => !IsAllowed<T>();
        public bool IsBlocked(ICondition condition)
            => !IsAllowed(condition);
        
        public int GetBlockCount<T>() where T : ICondition
        {
            _blocks.TryGetValue(ConditionRegistry.GetId<T>(), out int count);
            return count;
        }
        public int GetBlockCount(ICondition condition) 
        {
            _blocks.TryGetValue(condition.Id, out int count);
            return count;
        }
        
        public GateModifiable<bool> GetGate<T>() where T : ICondition
        {
            int id = ConditionRegistry.GetId<T>();
            if (!_gates.TryGetValue(id, out var b))
            {
                b = new GateModifiable<bool>(true);
                _gates[id] = b;
            }
            return b;
        }        
        public GateModifiable<bool> GetGate(ICondition condition)
        {            
            int id = condition.Id;
            if (!_gates.TryGetValue(id, out var b))
            {
                b = new GateModifiable<bool>(true);
                _gates[id] = b;
            }
            return b;
        }
        public ModifierDelegateHandler<bool> AddGate<T>(ModifierDelegate<bool> predicate, GateGeneral stage) where T : ICondition
        {
            int id = ConditionRegistry.GetId<T>();
            if (!_gates.TryGetValue(id, out var b))
            {
                b = new GateModifiable<bool>(true);
                _gates[id] = b;
            }
            bool before = IsAllowedInternal(id);
            var handle = b.Add(predicate, stage);
            NotifyIfChanged(id, before);
            return handle;
        }
        public ModifierDelegateHandler<bool> AddGate(ICondition condition, ModifierDelegate<bool> predicate, GateGeneral stage)
        {
            int id = condition.Id;
            if (!_gates.TryGetValue(id, out var b))
            {
                b = new GateModifiable<bool>(true);
                _gates[id] = b;
            }
            bool before = IsAllowedInternal(id);
            var handle = b.Add(predicate, stage);
            NotifyIfChanged(id, before);
            return handle;
        }
        public void RemoveGate<T>(ModifierDelegateHandler<bool> handle) where T : ICondition
        {
            int id = ConditionRegistry.GetId<T>();
            if (!_gates.TryGetValue(id, out var b)) return;
            bool before = IsAllowedInternal(id);
            b.Remove(handle);
            NotifyIfChanged(id, before);
        }
        public void RemoveGate(ICondition condition, ModifierDelegateHandler<bool> handle)
        {
            int id = condition.Id;
            if (!_gates.TryGetValue(id, out var b)) return;
            bool before = IsAllowedInternal(id);
            b.Remove(handle);
            NotifyIfChanged(id, before);
        }
        void NotifyIfChanged(int id, bool before)
        {
            bool after = IsAllowedInternal(id);
            if (before == after) return;
            var condition = ConditionRegistry.GetInstance(ConditionRegistry.GetTypeById(id));
            if (!after) OnBlocked?.Invoke(condition);
            else OnUnblocked?.Invoke(condition);
        }

        public event Action<ICondition> OnBlocked;
        public event Action<ICondition> OnUnblocked;
    }
}