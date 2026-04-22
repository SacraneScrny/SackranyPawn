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
        public Limb[] Default = { new StatHandler(), new ConditionHandler() };
        
        public bool IsStarted { get; private set; }
        public bool IsDisposed { get; private set; }
        public IEnumerable<Limb> GetLimbs() => _limbs.Values;
        
        public void Start()
        {
            if (IsStarted) return;
            IsStarted = true;
            Add(Default);
        }

        #region MODULES
        readonly Dictionary<int, Limb> _limbs = new Dictionary<int, Limb>();

        public bool TryAdd(Limb limb, out Limb result)
        {
            if (Add(limb))
            {
                result = Get(limb.GetType());
                return true;
            }
            result = null;
            return false;
        }
        public bool Add(Limb limb)
        {
            if (!IsDynamic) return false;

            if (!CreateAndRegister(limb, out var instance))
                return false;

            if (!DependencyCheck(instance))
            {
                RemoveInternal(LimbRegistry.GetId(limb.GetType()));
                return false;
            }

            instance.Awake();
            ActivateModule(instance);
            return true;
        }
        public bool Add(Limb[] limbs)
        {
            if (limbs.Length == 0) return true;
            if (!IsDynamic && _limbs.Count > 0) return false;

            bool allAdded = true;
            limbs = limbs.OrderBy(x => LimbReflectionCache.GetMetadata(x.GetType()).UpdateOrder).ToArray();

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
            while (!dependenciesSolved && _limbs.Count > 0 && tempLimbs.Count > 0)
            {
                dependenciesSolved = true;
                for (int i = tempLimbs.Count - 1; i >= 0; i--)
                {
                    if (DependencyCheck(tempLimbs[i].limb)) continue;
                    dependenciesSolved = false;
                    RemoveInternal(tempLimbs[i].id);
                    tempLimbs.RemoveAt(i);
                    allAdded = false;
                }
            }

            for (int i = 0; i < tempLimbs.Count; i++)
                tempLimbs[i].limb.Awake();
            for (int i = 0; i < tempLimbs.Count; i++)
                ActivateModule(tempLimbs[i].limb);

            return allAdded;
        }
        bool CreateAndRegister(Limb limb, out Limb instance)
        {
            var id = LimbRegistry.GetId(limb.GetType());
            if (_limbs.TryGetValue(id, out instance))
            {
                OnTryToAddAlreadyExist?.Invoke(instance);
                return false;
            }

            instance = limb;
            instance.FillPawn(Pawn);
            instance.FillBody(this);
            _limbs.Add(id, instance);
            return true;
        }
        void ActivateModule(Limb instance)
        {
            if (instance is IUpdateLimb u) _updateModules.Add(u);
            if (instance is IFixedUpdateLimb f) _fixedUpdateModules.Add(f);
            if (instance is ILateUpdateLimb l) _lateUpdateModules.Add(l);
            instance.Start();
            OnModuleAdded?.Invoke(instance);
        }
        
        public bool Remove<T>() where T : Limb
        {
            if (!IsDynamic) return false;
            return RemoveInternal(LimbRegistry.GetId<T>());
        }
        public bool Remove<T>(T module) where T : Limb
        {
            if (!IsDynamic) return false;
            return RemoveInternal(LimbRegistry.GetId(module.GetType()));
        }
        public bool Remove(Type type)
        {
            if (!IsDynamic) return false;
            return RemoveInternal(LimbRegistry.GetId(type));
        }
        
        bool RemoveInternal(int id)
        {
            if (!_limbs.ContainsKey(id))
                return false;

            var toRemove = new List<int>(4) { id };

            for (int i = 0; i < toRemove.Count; i++)
            {
                var removingType = LimbRegistry.GetTypeById(toRemove[i]);
                foreach (var (moduleId, module) in _limbs)
                {
                    if (toRemove.Contains(moduleId)) continue;
                    if (HasNonOptionalDepOn(module, removingType))
                        toRemove.Add(moduleId);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
                if (_limbs.TryGetValue(toRemove[i], out var m))
                    RemoveSingle(toRemove[i], m);

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
            _limbs.Remove(id);
            OnModuleRemoved?.Invoke(instance);
            instance.Dispose();
        }

        public void RemoveAll()
        {
            if (!IsDynamic) return;
            foreach (var module in _limbs.Values)
            {
                OnModuleRemoved?.Invoke(module);
                module.Reset();
                module.Dispose();
            }
            _updateModules.Clear();
            _fixedUpdateModules.Clear();
            _lateUpdateModules.Clear();
            _limbs.Clear();
        }
        
        public bool Has<T>() where T : Limb
            => _limbs.ContainsKey(LimbRegistry.GetId<T>());
        public bool Has(Type type) 
            => _limbs.ContainsKey(LimbRegistry.GetId(type));
        
        public T Get<T>() where T : Limb
        {
            if (_limbs.TryGetValue(LimbRegistry.GetId<T>(), out var instance))
                return (T)instance;
            return GetAssignable<T>();
        }
        public Limb Get(Type type)
        {
            if (_limbs.TryGetValue(LimbRegistry.GetId(type), out var instance))
                return instance;
            return GetAssignable(type);
        }
        
        public T GetAssignable<T>() where T : Limb
        {
            foreach (var module in _limbs.Values)
                if (module is T t)
                    return t;
            return null;
        }
        public Limb GetAssignable(Type type)
        {
            foreach (var module in _limbs.Values)
                if (type.IsAssignableFrom(module.GetType()))
                    return module;
            return null;
        }
        public Limb[] GetAllAssignable(Type type)
        {
            var modules = new List<Limb>();
            foreach (var module in _limbs.Values)
                if (type.IsAssignableFrom(module.GetType()))
                    modules.Add(module);
            return modules.ToArray();
        }
        
        public bool TryGet<T>(out T result, bool tryAssignable = false) where T : Limb
        {
            if (_limbs.TryGetValue(LimbRegistry.GetId<T>(), out var module))
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
            if (_limbs.TryGetValue(LimbRegistry.GetId(type), out var module))
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
        public bool TryGetAssignable<T>(out Limb result)
        {
            foreach (var module in _limbs.Values.Where(module => module is T))
            {
                result = module;
                return true;
            }
            result = null;
            return false;
        }
        public bool TryGetAssignable(Type type, out Limb result)
        {
            foreach (var module in _limbs.Values.Where(module => type.IsAssignableFrom(module.GetType())))
            {
                result = module;
                return true;
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
        
        /// <summary>
        /// Complete reset and reinitialization of default modules
        /// </summary>
        public void Reinitialize()
        {
            if (IsDisposed) return;
            if (!IsDynamic)
            {
                ResetState();
                return;
            }
            RemoveAll();
            IsStarted = false;
            OnModuleAdded = null;
            OnModuleRemoved = null;
            OnTryToAddAlreadyExist = null;
            OnModulesRestarted?.Invoke();
            Start();
        }
        
        /// <summary>
        /// Just reset the modules, no reassembly
        /// </summary>
        public void ResetState()
        {
            if (!IsStarted) return;
            if (IsDisposed) return;
            foreach (var module in _limbs.Values)
                module.Reset();
            OnModuleAdded = null;
            OnModuleRemoved = null;
            OnTryToAddAlreadyExist = null;
            OnModulesReset?.Invoke();
            foreach (var module in _limbs.Values)
                module.Start();
        }
        
        public void Dispose()
        {
            if (IsDisposed) return;
            RemoveAll();
            OnModuleAdded = null;
            OnModuleRemoved = null;
            OnTryToAddAlreadyExist = null;
            IsDisposed = true;
        }
        
        public event Action<Limb> OnModuleAdded;
        public event Action OnModulesRestarted;
        public event Action OnModulesReset;
        public event Action<Limb> OnTryToAddAlreadyExist;
        public event Action<Limb> OnModuleRemoved;

        #if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            foreach (var module in _limbs.Values)
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