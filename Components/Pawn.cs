using System;
using System.Collections.Generic;

using ModifiableVariable;

using SackranyPawn.Cache;
using SackranyPawn.Entities;
using SackranyPawn.Entities.Modules;
using SackranyPawn.Managers;
using SackranyPawn.Traits.PawnEvents;
using SackranyPawn.Traits.Tags;

using UnityEngine;

namespace SackranyPawn.Components
{
    [AddComponentMenu("Sackrany/Pawn")]
    public class Pawn : MonoBehaviour, IEquatable<Pawn>
    {
        public bool DebugTracing;
        public bool IsTracing() => DebugTracing;
        
        [SerializeField] bool WorkByDefault;
        public PawnTag Tag;
        public PawnEventBus Event;

        [SerializeField] Body Body;
        public Body GetController() => Body;
        public IEnumerable<Limb> GetModules() => Body?.GetLimbs();
        
        public PawnArchetype Archetype => _archetype;
        [SerializeField] PawnArchetype _archetype;
        
        public TeamInfo Team { get; private set; }
        
        public Modifiable<float> TimeFlow { get; private set; }
        public bool IsWorking { get; private set; }
        public bool IsActive => IsWorking && gameObject.activeSelf && gameObject.activeInHierarchy;
        public int Hash { get; private set; }
        
        bool _isInitialized;
        bool _isQuitting;
        
        void OnValidate()
        {
            _archetype = new PawnArchetype(this);
        }
        
        void Awake()
        {
            Initialize();
            if (WorkByDefault)
            {
                StartWork();
            }
        }
        public void Initialize()
        {
            if (_isInitialized) return;
            Application.quitting += OnApplicationQuitting;
            
            Hash = PawnHash.GetId();
            
            Tag ??= new ();
            Tag.Initialize(this);
            
            Event ??= new ();
            
            _archetype = new (this);
            TimeFlow = new (1);
            Team = new (Tag, true);

            Body ??= new ();
            Body.FillPawn(this);
            Body.FillBody(Body);
            _isInitialized = true;
        }
        void OnApplicationQuitting() => _isQuitting = true;
        void Start()
        {
            Body.Start();
        }

        #region MODULES
        public bool Add(Limb limb, out Limb result) => Body.Add(limb, out result);
        public bool Add(Limb limb) => Body.Add(limb);
        public bool Add(Limb[] limbs) => Body.Add(limbs);
        
        public bool Has<T>() where T : Limb => Body.Has<T>();
        public bool Has(Type type) => Body.Has(type);
        
        public T Get<T>() where T : Limb => Body.Get<T>();
        public Limb Get(Type type) => Body.Get(type); 
        
        public bool Remove<T>() where T : Limb => Body.Remove<T>();
        public bool Remove<T>(T module) where T : Limb => Remove<T>();
        public bool Remove(Type type) => Body.Remove(type);

        public void RemoveAll() => Body.RemoveAll();
        
        public bool TryGet<T>(out T result) where T : Limb => Body.TryGet(out result);
        public bool TryGet(Type type, out Limb result) => Body.TryGet(type, out result);
        #endregion

        #region UPDATE
        public void OnUpdate(float dt)
        {
            if (!IsWorking) return;
            Body.Update(dt * TimeFlow);
        }
        public void OnFixedUpdate(float dt)
        {
            if (!IsWorking) return;
            Body.FixedUpdate(dt * TimeFlow);
        }
        public void OnLateUpdate(float dt)
        {
            if (!IsWorking) return;
            Body.LateUpdate(dt * TimeFlow);
        }
        #endregion

        #region SERIALIZATION
        public bool IsDeserialized {get; private set;}
        public void MarkAsDeserialized()
        {
            IsDeserialized = true;
        }
        #endregion
        
        public void StartWork()
        {
            if (IsWorking) return;
            IsWorking = true;
            PawnRegister.RegisterPawn(this);
            OnStartWorking?.Invoke(this);
        }
        public void StopWork()
        {
            if (!IsWorking) return;
            IsWorking = false;
            PawnRegister.UnregisterPawn(this);
            OnStopWorking?.Invoke(this);
        }
        
        public void ResetState()
        {
            if (!Application.isPlaying) return;
            OnReset?.Invoke(this);
            Tag.Reset();
            Event.Reset();
            TimeFlow.Clear();
            Body.ResetState();
        }
        public void Reinitialize()
        {
            OnRestart?.Invoke(this);
            Tag.Reset();
            Event.Reset();
            TimeFlow.Clear();
            Body.Reinitialize();
        }
        
        public void OnPooled()
        {
            gameObject.SetActive(true);
            Reinitialize();
            if (WorkByDefault) StartWork();
        }
        public void OnReleased()
        {
            StopWork();
            gameObject.SetActive(false);
        }
        
        public void SetWorkByDefault(bool value) => WorkByDefault = value;
        public void Run()
        {
            WorkByDefault = true;
            StartWork();
        }
        public void UpdateTeam()
        {
            Team = new (Tag, true);
        }
        
        public event Action<Pawn> OnStartWorking;
        public event Action<Pawn> OnStopWorking;
        public event Action<Pawn> OnRestart;
        public event Action<Pawn> OnReset;

        #region EQUALS
        public bool Equals(Pawn other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Hash == other.Hash;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((Pawn)obj);
        }
        public override int GetHashCode()
        {
            return Hash;
        }

        public static bool operator ==(Pawn left, Pawn right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null)) return false;
            return left.Equals(right);
        }
        public static bool operator !=(Pawn left, Pawn right)
            => !(left == right);
        #endregion
        
        void OnDestroy()
        {
            if (_isQuitting) return;
            Body.Dispose();
            PawnRegister.UnregisterPawn(this);
            Application.quitting -= OnApplicationQuitting;
        }
        
        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Body.OnDrawGizmos();    
        }
        #endif
    }
}