using System;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Cache;
using SackranyPawn.Entities.Modules.ModuleComposition;
using SackranyPawn.Traits.Conditions;
using SackranyPawn.Traits.Stats;

using UnityEngine;

namespace SackranyPawn.Entities.Modules
{
    [Serializable]
    public sealed class Body : PawnBase, IDisposable
    {
        public BodyMode Mode = BodyMode.Dynamic;
        bool IsDynamic => Mode == BodyMode.Dynamic;
        
        [SerializeField][SerializeReference][SubclassSelector] 
        List<Limb> Limbs = new(){ new StatHandler(), new ConditionHandler() };
        
        public bool IsStarted { get; private set; }
        public bool IsDisposed { get; private set; }
        public IReadOnlyList<Limb> GetLimbs() => Limbs;
        
        public void Start()
        {
            if (IsStarted) return;
            IsStarted = true;
                
            Add(Limbs.ToArray(), false);
        }

        #region LIMBS
        readonly Dictionary<int, Limb> _limbMap = new ();

        public bool TryAdd(Limb limb, out Limb result, bool asTemp = true)
        {
            if (Add(limb, asTemp))
            {
                result = Get(limb.GetType());
                return true;
            }
            result = null;
            return false;
        }
        public bool Add(Limb limb, bool asTemp = true)
        {
            if (!IsDynamic) return false;
            if (limb == null) return false;

            if (!CreateAndRegister(limb, out var instance))
                return false;

            if (!DependencyCheck(instance))
            {
                RemoveInternal(LimbRegistry.GetId(limb.GetType()));
                return false;
            }

            if (asTemp) instance.MarkTemporary();
            instance.Awake();
            ActivateModule(instance);
            return true;
        }
        public bool Add(Limb[] limbs, bool asTemp = true)
        {
            if (limbs == null || limbs.Length == 0) return false;
            if (!IsDynamic && _limbMap.Count > 0) return false;

            bool allAdded = true;
            Array.Sort(limbs, (a, b) =>
                LimbReflectionCache.GetMetadata(a.GetType()).UpdateOrder
                    .CompareTo(LimbReflectionCache.GetMetadata(b.GetType()).UpdateOrder));

            var tempLimbs = new List<(Limb limb, int id)>(limbs.Length);
            for (int i = 0; i < limbs.Length; i++)
            {
                if (!CreateAndRegister(limbs[i], out var instance))
                {
                    allAdded = false;
                    continue;
                }
                tempLimbs.Add((instance, LimbRegistry.GetId(limbs[i].GetType())));
            }

            bool dependenciesSolved = false;
            while (!dependenciesSolved && _limbMap.Count > 0 && tempLimbs.Count > 0)
            {
                dependenciesSolved = true;
                for (int i = tempLimbs.Count - 1; i >= 0; i--)
                {
                    if (DependencyCheck(tempLimbs[i].limb)) continue;
                    dependenciesSolved = false;
                    RemoveInternal(tempLimbs[i].id);
                    tempLimbs.RemoveAt(i);  
                    
                    for (int j = tempLimbs.Count - 1; j >= 0; j--)
                        if (!_limbMap.ContainsKey(tempLimbs[j].id))
                            tempLimbs.RemoveAt(j);
                    
                    allAdded = false;
                }
            }

            for (int i = 0; i < tempLimbs.Count; i++)
            {
                if (asTemp)
                    tempLimbs[i].limb.MarkTemporary();
                tempLimbs[i].limb.Awake();
            }
            for (int i = 0; i < tempLimbs.Count; i++)
                ActivateModule(tempLimbs[i].limb);

            return allAdded;
        }
        
        bool CreateAndRegister(Limb limb, out Limb instance)
        {
            var id = LimbRegistry.GetId(limb.GetType());
            if (_limbMap.TryGetValue(id, out instance))
            {
                TriedToAddAlreadyExist?.Invoke(instance);
                return false;
            }

            instance = limb;
            instance.FillPawn(Pawn);
            instance.FillBody(this);
            _limbMap.Add(id, instance);
            if (!Limbs.Contains(instance)) Limbs.Add(instance);
            return true;
        }
        void ActivateModule(Limb instance)
        {
            if (instance is IUpdateLimb u) _updateModules.Add(u);
            if (instance is IFixedUpdateLimb f) _fixedUpdateModules.Add(f);
            if (instance is ILateUpdateLimb l) _lateUpdateModules.Add(l);
            instance.Start();
            LimbAdded?.Invoke(instance);
        }
        
        public bool Remove<T>() where T : Limb
        {
            if (!IsDynamic) return false;
            return RemoveInternal(LimbRegistry.GetId<T>());
        }
        public bool Remove<T>(T limb) where T : Limb
        {
            if (!IsDynamic) return false;
            if (limb == null) return false;
            return RemoveInternal(LimbRegistry.GetId(limb.GetType()));
        }
        public bool Remove(Type type)
        {
            if (!IsDynamic) return false;
            return RemoveInternal(LimbRegistry.GetId(type));
        }
        
        bool RemoveInternal(int id)
        {
            if (!_limbMap.ContainsKey(id))
                return false;

            var toRemove = new HashSet<int> { id };
            var queue = new List<int>(4) { id };

            for (int i = 0; i < queue.Count; i++)
            {
                var removingType = LimbRegistry.GetTypeById(queue[i]);
                foreach (var (limbId, limb) in _limbMap)
                {
                    if (toRemove.Contains(limbId)) continue;
                    if (!HasNonOptionalDepOn(limb, removingType)) continue;
                    toRemove.Add(limbId);
                    queue.Add(limbId);
                }
            }

            foreach (var removeId in toRemove)
                if (_limbMap.TryGetValue(removeId, out var m))
                    RemoveSingle(removeId, m);

            return true;
        }
        static bool HasNonOptionalDepOn(Limb limb, Type removedType)
        {
            var deps = LimbReflectionCache.GetMetadata(limb.GetType()).Dependencies;
            for (int i = 0; i < deps.Length; i++)
            {
                if (deps[i].IsOptional) continue;
                var checkType = deps[i].IsArray ? deps[i].ElementType : deps[i].FieldType;
                if (checkType != null && checkType.IsAssignableFrom(removedType))
                    return true;
            }
            return false;
        }
        void RemoveSingle(int id, Limb instance)
        {
            if (instance is IUpdateLimb u) _updateModules.Remove(u);
            if (instance is IFixedUpdateLimb f) _fixedUpdateModules.Remove(f);
            if (instance is ILateUpdateLimb l) _lateUpdateModules.Remove(l);
            _limbMap.Remove(id);
            Limbs.Remove(instance);
            if (instance.IsStarted)
                LimbRemoved?.Invoke(instance);
            instance.Dispose();
        }

        public void RemoveAll()
        {
            if (!IsDynamic) return;
            foreach (var limb in _limbMap.Values)
            {
                if (limb.IsStarted)
                    LimbRemoved?.Invoke(limb);
                limb.Reset();
                limb.Dispose();
            }
            _updateModules.Clear();
            _fixedUpdateModules.Clear();
            _lateUpdateModules.Clear();
            _limbMap.Clear();
            Limbs.Clear();
        }
        
        public bool Has<T>(bool tryAssignable = false) where T : Limb
        {
            if (_limbMap.ContainsKey(LimbRegistry.GetId<T>())) return true;
            if (tryAssignable) return GetAssignable<T>() != null;
            return false;
        }
        public bool Has(Type type, bool tryAssignable = false)
        {
            if (_limbMap.ContainsKey(LimbRegistry.GetId(type))) return true;
            if (tryAssignable) return GetAssignable(type) != null;
            return false;
        }
        
        public T Get<T>() where T : Limb
        {
            if (_limbMap.TryGetValue(LimbRegistry.GetId<T>(), out var instance))
                return (T)instance;
            return GetAssignable<T>();
        }
        public Limb Get(Type type)
        {
            if (_limbMap.TryGetValue(LimbRegistry.GetId(type), out var instance))
                return instance;
            return GetAssignable(type);
        }
        
        public T GetAssignable<T>() where T : Limb
        {
            foreach (var module in _limbMap.Values)
                if (module is T t)
                    return t;
            return null;
        }
        public Limb GetAssignable(Type type)
        {
            foreach (var module in _limbMap.Values)
                if (type.IsAssignableFrom(module.GetType()))
                    return module;
            return null;
        }
        public Limb[] GetAllAssignable(Type type)
        {
            var modules = new List<Limb>();
            foreach (var module in _limbMap.Values)
                if (type.IsAssignableFrom(module.GetType()))
                    modules.Add(module);
            return modules.ToArray();
        }
        public Component[] GetAllAssignableComponents(Type type)
        {
            var ret = new List<Component>();
            foreach (var comp in Pawn.GetComponentsInChildren(type))
                if (comp != null)
                    ret.Add(comp);
            return ret.ToArray();
        }
        
        public bool TryGet<T>(out T result, bool tryAssignable = false) where T : Limb
        {
            if (_limbMap.TryGetValue(LimbRegistry.GetId<T>(), out var module))
            {
                result = (T)module;
                return true;
            }
            if (tryAssignable && TryGetAssignable<T>(out var resultAssignable))
            {
                result = (T)resultAssignable;
                return true;
            }
            result = null;
            return false;
        }
        public bool TryGet(Type type, out Limb result, bool tryAssignable = false)
        {
            if (_limbMap.TryGetValue(LimbRegistry.GetId(type), out var module))
            {
                result = module;
                return true;
            }
            if (tryAssignable && TryGetAssignable(type, out var resultAssignable))
            {
                result = resultAssignable;
                return true;
            }
            result = null;
            return false;
        }
        public bool TryGetAssignable<T>(out T result) where T : Limb
        {
            foreach (var module in _limbMap.Values)
            {
                if (module is T t)
                {
                    result = t;
                    return true;
                }
            }
            result = null;
            return false;
        }
        public bool TryGetAssignable(Type type, out Limb result)
        {
            foreach (var module in _limbMap.Values)
            {
                if (type.IsAssignableFrom(module.GetType()))
                {
                    result = module;
                    return true;
                }
            }

            result = null;
            return false;
        }

        bool DependencyCheck(Limb m) => DependencyInjector.InjectDependencies(m, this) && m.OnDependencyCheck();
        #endregion

        #region UPDATE
        readonly List<IUpdateLimb> _updateModules = new List<IUpdateLimb>();
        readonly List<IFixedUpdateLimb> _fixedUpdateModules = new List<IFixedUpdateLimb>();
        readonly List<ILateUpdateLimb> _lateUpdateModules = new List<ILateUpdateLimb>();

        public void Update(float deltaTime)
        {
            if (!IsStarted || IsDisposed) return;
            for (int i = 0; i < _updateModules.Count; i++)
                _updateModules[i].OnUpdate(deltaTime);
        }
        public void FixedUpdate(float deltaTime)
        {
            if (!IsStarted || IsDisposed) return;
            for (int i = 0; i < _fixedUpdateModules.Count; i++)
                _fixedUpdateModules[i].OnFixedUpdate(deltaTime);
        }
        public void LateUpdate(float deltaTime)
        {
            if (!IsStarted || IsDisposed) return;
            for (int i = 0; i < _lateUpdateModules.Count; i++)
                _lateUpdateModules[i].OnLateUpdate(deltaTime);
        }
        #endregion
        
        public void Reset()
        {
            if (!IsStarted) return;
            if (IsDisposed) return;
            
            var _toRemove = Limbs.Where(x => x.IsTemporary).ToArray();
            foreach (var limb in _toRemove)
                Remove(limb);
            
            foreach (var module in _limbMap.Values)
                module.Reset();
            
            LimbsReseted?.Invoke();
            foreach (var module in _limbMap.Values)
                module.Start();
        }
        public void Dispose()
        {
            if (IsDisposed) return;
            foreach (var limb in _limbMap.Values)
                limb.Dispose();
            Limbs.Clear();
            _limbMap.Clear();
            LimbAdded = null;
            LimbRemoved = null;
            TriedToAddAlreadyExist = null;
            LimbsReseted = null;
            IsDisposed = true;
        }
        
        public event Action<Limb> LimbAdded;
        public event Action LimbsReseted;
        public event Action<Limb> TriedToAddAlreadyExist;
        public event Action<Limb> LimbRemoved;

        #if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            foreach (var module in _limbMap.Values)
                module.OnDrawGizmos();
        }
        #endif
    }

    public enum BodyMode
    {
        [InspectorName("Dynamic (Add/Remove enabled)")] Dynamic,
        [InspectorName("Sealed (Reset only)")] Sealed
    }
}