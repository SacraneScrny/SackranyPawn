using System;
using System.Collections.Generic;
using System.Threading;

using Cysharp.Threading.Tasks;

using R3;

using SackranyPawn.Traits.Fluxes.Cache;

using UnityEngine;

namespace SackranyPawn.Traits.Fluxes.Entities
{
    public interface FluxHandle
    {
        int Id { get; }
        bool IsDisposed { get; }
        bool IsStarted { get; }
        ReadOnlyReactiveProperty<int> Amount { get; }
        ReadOnlyReactiveProperty<float> Progress { get; }
        CancellationToken Token { get; }
        
        bool ChangeAmount(int diff);
        bool IncreaseAmount(int value = 1) => ChangeAmount(Mathf.Abs(value));
        bool DecreaseAmount(int value = 1) => ChangeAmount(-Mathf.Abs(value));
        
        void ForceStop();

        internal Flux GetFlux();
    }
    
    [Serializable]
    public abstract class Flux : FluxHandle, ICloneable, IDisposable 
    {
        #if UNITY_EDITOR
        public int DebugAmount;
        public float DebugProgress;
        #endif
        
        public abstract int Id { get; }
        public bool IsDisposed { get; private set; }
        public bool IsStarted { get; private set; }

        ReactiveProperty<int> _amount;
        ReactiveProperty<float> _progress;
        
        public ReadOnlyReactiveProperty<int> Amount { get; private set; }
        public ReadOnlyReactiveProperty<float> Progress { get; private set; }
        
        protected FluxHandler Handler { get; private set; }
        protected float DeltaTime => Handler.CachedDeltaTime;
        protected float FixedDeltaTime => Handler.CachedFixedDeltaTime;
        
        CancellationTokenSource _localCts;
        public CancellationToken Token { get; private set; }

        CompositeDisposable _disposables;
        
        public void Initialize(
            FluxHandler handler, 
            int amount)
        {
            _disposables = new();
            Handler = handler;
            
            _amount = new(amount);
            _progress = new(0);
            
            _localCts = new CancellationTokenSource();
            var linked = CancellationTokenSource.CreateLinkedTokenSource(
                handler.ModuleToken,
                _localCts.Token
            );
            Token = linked.Token;
            _disposables.Add(linked);
            
            _disposables.Add(_localCts);
            _disposables.Add(_amount);
            _disposables.Add(_progress);

            Amount = _amount.ToReadOnlyReactiveProperty();
            Progress = _progress.ToReadOnlyReactiveProperty();
            _disposables.Add(Amount);
            _disposables.Add(Progress);
            
            #if UNITY_EDITOR
            _disposables.Add(Amount.Subscribe(x => DebugAmount = x));
            _disposables.Add(Progress.Subscribe(x => DebugProgress = x));
            #endif
        }
        public void Start()
        {
            if (IsDisposed) return;
            if (IsStarted) return;
            OnStart();
            IsStarted = true;
        }
        protected abstract void OnStart();
        public void ForceStop()
        {
            if (IsDisposed) return;
            if (!IsStarted) return;
            OnForceStop();
            StateChanged?.Invoke(this, FluxState.ForceStopped);
            Handler.RemoveFlux(this);
        }
        protected virtual void OnForceStop() { }

        protected virtual bool TickProgress(float dt)
        {
            _progress.Value += dt;
            StateChanged?.Invoke(this, FluxState.ProgressChanged);
            if (_progress.Value >= 1)
            {
                StateChanged?.Invoke(this, FluxState.ProgressReached);
                _progress.Value = 0;
                return true;
            }
            return false;
        }
        protected virtual bool SetProgress(float value)
        {
            _progress.Value = Mathf.Clamp01(value);
            StateChanged?.Invoke(this, FluxState.ProgressChanged);
            if (_progress.Value >= 1)
            {
                StateChanged?.Invoke(this, FluxState.ProgressReached);
                _progress.Value = 0;
                return true;
            }
            return false;
        }

        public bool ChangeAmount(int diff)
        {
            if (IsDisposed) return false;
            if (!IsStarted) return false;
            
            if (diff == 0) return false;
            if (_amount.Value == 0) return false;
            OnAmountChangesCome(ref diff);
            if (diff == 0) return false;
            
            diff -= Mathf.Min(_amount.Value + diff, 0);
            _amount.Value += diff;
            
            OnAmountChanged(diff);
            StateChanged?.Invoke(this, FluxState.AmountChanged);
            
            if (diff < 0)
            {
                OnAmountDecreased(Mathf.Abs(diff));
                StateChanged?.Invoke(this, FluxState.AmountDecreased);
            }
            else
            {
                OnAmountIncreased(diff);
                StateChanged?.Invoke(this, FluxState.AmountIncreased);
            }

            if (_amount.Value <= 0)
            {
                OnOutOfAmount();
                StateChanged?.Invoke(this, FluxState.OutOfAmount);
                Handler.RemoveFlux(this);
            }
            return true;
        }
        protected virtual void OnAmountChangesCome(ref int diff) { }
        protected virtual void OnAmountChanged(int diff) { }
        protected virtual void OnAmountIncreased(int value) { }
        protected virtual void OnAmountDecreased(int value) { }
        protected virtual void OnOutOfAmount() { }

        internal void StartTask(Func<CancellationToken, UniTask> task)
        {
            RunTask(task).Forget();
        }
        async UniTaskVoid RunTask(Func<CancellationToken, UniTask> task)
        {
            try
            {
                await task(Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        public void Dispose()
        {
            if (IsDisposed) return;
            StateChanged?.Invoke(this, FluxState.Disposing);
            OnDisposing();
            
            _localCts?.Cancel();
            
            IsDisposed = true;
            IsStarted = false;
            StateChanged = null;
            
            _disposables?.Dispose();
            _disposables?.Clear();
        }
        
        protected virtual void OnDisposing() { }
        protected void Track(IDisposable disposable) => _disposables.Add(disposable);
        
        public object Clone()
        {
            var clone = (Flux)MemberwiseClone();
            OnClone(clone);
            return clone;
        }
        protected virtual void OnClone(Flux clone) { }

        internal event Action<Flux, FluxState> StateChanged;
        Flux FluxHandle.GetFlux() => this;
    }
    
    [Serializable]
    public abstract class Flux<TSelf> : Flux 
        where TSelf : Flux<TSelf>
    {
        public sealed override int Id => FluxRegistry.GetId<TSelf>();
        
        protected sealed override void OnClone(Flux clone) => OnClone((TSelf)clone);
        protected virtual void OnClone(TSelf clone) { }
    }

    public enum FluxState
    {
        None = 0,
        ProgressChanged = 14,
        ProgressReached = 15,
        ForceStopped = 20,
        AmountChanged = 25,
        AmountIncreased = 27,
        AmountDecreased = 29,
        OutOfAmount = 30,
        Disposing = 200,
    }
}