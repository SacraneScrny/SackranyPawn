using System;
using System.Threading;

namespace SackranyPawn.Entities.Modules
{
    [Serializable]
    public abstract class Limb : PawnBase, IDisposable
    {
        public bool IsAwaken { get; private set; }
        public bool IsStarted { get; private set; }
        public bool IsDisposed { get; private set; }

        public void Awake()
        {
            if (IsAwaken) return;
            if (IsDisposed) return;
            IsAwaken = true;
            OnAwakeInternal();
            OnAwake();
        }
        public void Start()
        {
            if (!IsAwaken) return;
            if (IsStarted) return;
            if (IsDisposed) return;
            OnStartInternal();
            IsStarted = true;
            OnStart();
        }
        
        public virtual bool OnDependencyCheck() => true;
        
        public void Reset()
        {
            if (!IsStarted) return;
            if (IsDisposed) return;
            OnResetInternal();
            IsStarted = false;
            OnReset();
        }
        public void Dispose()
        {
            if (IsDisposed) return;
            OnDisposeInternal();
            OnDispose();
            IsDisposed = true;
        }
        
        public bool TryAdd(Limb limb, out Limb result) => Body.TryAdd(limb, out result);
        public bool Add(Limb limb) => Body.Add(limb);
        public bool Add(Limb[] limbs) => Body.Add(limbs);
        
        public bool Remove<T>() where T : Limb => Body.Remove<T>();
        public bool Remove<T>(T module) where T : Limb => Remove<T>();
        public bool Remove(Type type) => Body.Remove(type);

        public void RemoveAll() => Body.RemoveAll();
        
        public bool Has<T>() where T : Limb => Body.Has<T>();
        public bool Has(Type type) => Body.Has(type);
        
        public T Get<T>() where T : Limb => Body.Get<T>();
        public Limb Get(Type type) => Body.Get(type); 
        
        public bool TryGet<T>(out T result) where T : Limb => Body.TryGet(out result);
        public bool TryGet(Type type, out Limb result) => Body.TryGet(type, out result);
        
        public bool IsInitialized()
            => IsStarted && !IsDisposed;
        
        public virtual void OnDrawGizmos() { }
        
        private protected virtual void OnAwakeInternal() { }
        private protected virtual void OnStartInternal() { }
        private protected virtual void OnResetInternal() { }
        private protected virtual void OnDisposeInternal() { }
        
        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual void OnReset() { }
        protected virtual void OnDispose() { }
    }
    
    [Serializable]
    public class AsyncLimb : Limb
    {
        CancellationTokenSource _lifecycleCts;
        public virtual CancellationToken ModuleToken => _lifecycleCts?.Token ?? CancellationToken.None;

        private protected sealed override void OnAwakeInternal()
            => _lifecycleCts = new CancellationTokenSource();

        private protected sealed override void OnStartInternal()
        {
            if (_lifecycleCts == null || _lifecycleCts.IsCancellationRequested)
                _lifecycleCts = new CancellationTokenSource();
        }

        private protected sealed override void OnResetInternal()
            => _lifecycleCts?.Cancel();

        private protected sealed override void OnDisposeInternal()
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;
        }
    }
}