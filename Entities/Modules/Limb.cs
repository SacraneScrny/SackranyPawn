using System;
using System.Threading;

using SackranyPawn.Plugin.Cache;
using SackranyPawn.Plugin.Default;

namespace SackranyPawn.Entities.Modules
{
    [Serializable]
    public abstract class Limb : PawnBase, IDisposable
    {
        public bool IsTemporary { get; private set; }

        public bool IsAwaken { get; private set; }
        public bool IsStarted { get; private set; }
        public bool IsDisposed { get; private set; }

        public void MarkTemporary() => IsTemporary = true;
        public void Awake()
        {
            if (IsAwaken) return;
            if (IsDisposed) return;
            IsAwaken = true;

            var plugins = PluginRegistry.Get<LimbPlugins.ILimbAwaking>.Value;
            for (int i = 0; i < plugins.Length; i++)
                plugins[i].Execute(this);

            OnAwakeInternal();
            OnAwake();
        }
        public void Start()
        {
            if (!IsAwaken) return;
            if (IsStarted) return;
            if (IsDisposed) return;

            var plugins = PluginRegistry.Get<LimbPlugins.ILimbStarting>.Value;
            for (int i = 0; i < plugins.Length; i++)
                plugins[i].Execute(this);

            OnStartInternal();
            IsStarted = true;
            OnStart();
        }

        public virtual bool OnDependencyCheck() => true;

        public void Reset()
        {
            if (!IsStarted) return;
            if (IsDisposed) return;

            var plugins = PluginRegistry.Get<LimbPlugins.ILimbResetting>.Value;
            for (int i = 0; i < plugins.Length; i++)
                plugins[i].Execute(this);

            OnResetInternal();
            IsStarted = false;
            OnReset();
        }
        public void Dispose()
        {
            if (!IsAwaken)
            {
                IsStarted = false;
                IsAwaken = false;
                IsDisposed = true;
                OnDisposeBeforeAwaken();
                return;
            }
            if (IsDisposed) return;

            var plugins = PluginRegistry.Get<LimbPlugins.ILimbDisposing>.Value;
            for (int i = 0; i < plugins.Length; i++)
                plugins[i].Execute(this);

            OnDisposeInternal();
            OnDispose();
            IsStarted = false;
            IsAwaken = false;
            IsDisposed = true;
        }

        public bool TryAdd(Limb limb, out Limb result) => Body.TryAdd(limb, out result);
        public bool Add(Limb limb) => Body.Add(limb);
        public bool Add(Limb[] limbs) => Body.Add(limbs);

        public bool Remove<T>() where T : Limb => Body.Remove<T>();
        public bool Remove(Limb module) => Body.Remove(module);
        public bool Remove(Type type) => Body.Remove(type);

        public void RemoveAll() => Body.RemoveAll();

        public bool Has<T>(bool tryAssignable = false) where T : Limb => Body.Has<T>(tryAssignable);
        public bool Has(Type type, bool tryAssignable = false) => Body.Has(type, tryAssignable);

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
        protected virtual void OnDisposeBeforeAwaken() { }
    }

    [Serializable]
    public class AsyncLimb : Limb
    {
        CancellationTokenSource _lifecycleCts;
        public CancellationToken ModuleToken
        {
            get
            {
                if (_lifecycleCts != null && !IsDisposed)
                    return _lifecycleCts.Token;
                return new CancellationToken(true);
            }
        }

        private protected sealed override void OnAwakeInternal()
            => _lifecycleCts = new CancellationTokenSource();

        private protected sealed override void OnStartInternal()
        {
            if (_lifecycleCts == null || _lifecycleCts.IsCancellationRequested)
            {
                _lifecycleCts?.Dispose();
                _lifecycleCts = new CancellationTokenSource();
            }
        }

        private protected sealed override void OnResetInternal() => _lifecycleCts?.Cancel();
        private protected sealed override void OnDisposeInternal()
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;
        }
    }
}