using System;
using System.Collections.Generic;

namespace SackranyPawn.Traits.Fluxes.Entities
{
    public sealed class FluxObserver : IDisposable
    {
        Flux _flux;
        Func<FluxHandle, bool> _where;

        readonly struct CallbackEntry
        {
            public readonly Action<FluxHandle> Callback;
            public readonly bool Once;
            public CallbackEntry(Action<FluxHandle> callback, bool once)
            {
                Callback = callback;
                Once = once;
            }
        }

        List<CallbackEntry> _progressChangedCallbacks;
        List<CallbackEntry> _progressReachedCallbacks;
        List<CallbackEntry> _forceStoppedCallbacks;
        List<CallbackEntry> _amountChangedCallbacks;
        List<CallbackEntry> _amountIncreasedCallbacks;
        List<CallbackEntry> _amountDecreasedCallbacks;
        List<CallbackEntry> _outOfAmountCallbacks;
        List<CallbackEntry> _disposingCallbacks;

        List<(int threshold, CallbackEntry entry)> _amountReachedCallbacks;
        List<(float threshold, CallbackEntry entry)> _progressReachedThresholdCallbacks;

        public FluxObserver(Flux flux)
        {
            _flux = flux;
            _flux.StateChanged += OnStateChanged;
        }

        void OnStateChanged(Flux f, FluxState state)
        {
            if (_disposed) return;
            if (_where != null && !_where(_flux)) return;

            switch (state)
            {
                case FluxState.ProgressChanged:
                    Call(ref _progressChangedCallbacks);
                    CallProgressReached();
                    break;
                case FluxState.ProgressReached:
                    Call(ref _progressReachedCallbacks);
                    break;
                case FluxState.ForceStopped:
                    Call(ref _forceStoppedCallbacks);
                    break;
                case FluxState.AmountChanged:
                    Call(ref _amountChangedCallbacks);
                    CallAmountReached();
                    break;
                case FluxState.AmountIncreased:
                    Call(ref _amountIncreasedCallbacks);
                    break;
                case FluxState.AmountDecreased:
                    Call(ref _amountDecreasedCallbacks);
                    break;
                case FluxState.OutOfAmount:
                    Call(ref _outOfAmountCallbacks);
                    break;
                case FluxState.Disposing:
                    Call(ref _disposingCallbacks);
                    Dispose();
                    break;
            }
        }
        void Call(ref List<CallbackEntry> callbacks)
        {
            if (callbacks == null) return;
            for (int i = callbacks.Count - 1; i >= 0; i--)
            {
                var entry = callbacks[i];
                entry.Callback(_flux);
                if (entry.Once) callbacks.RemoveAt(i);
            }
        }

        void CallAmountReached()
        {
            if (_amountReachedCallbacks == null) return;
            int current = _flux.Amount.CurrentValue;
            for (int i = _amountReachedCallbacks.Count - 1; i >= 0; i--)
            {
                var (threshold, entry) = _amountReachedCallbacks[i];
                if (current != threshold) continue;
                entry.Callback(_flux);
                if (entry.Once) _amountReachedCallbacks.RemoveAt(i);
            }
        }
        void CallProgressReached()
        {
            if (_progressReachedThresholdCallbacks == null) return;
            float current = _flux.Progress.CurrentValue;
            for (int i = _progressReachedThresholdCallbacks.Count - 1; i >= 0; i--)
            {
                var (threshold, entry) = _progressReachedThresholdCallbacks[i];
                if (current < threshold) continue;
                entry.Callback(_flux);
                if (entry.Once) _progressReachedThresholdCallbacks.RemoveAt(i);
            }
        }

        bool AlreadyDisposed()
        {
            if (_disposed) return true;
            if (_flux == null || _flux.IsDisposed)
            {
                Dispose();
                return true;
            }

            return false;
        }
        void Add(ref List<CallbackEntry> list, Action<FluxHandle> callback, bool once)
        {
            list ??= new();
            list.Add(new CallbackEntry(callback, once));
        }

        public FluxObserver OnProgressChanged(Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            Add(ref _progressChangedCallbacks, callback, once);
            return this;
        }
        public FluxObserver OnProgressReached(float threshold, Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            _progressReachedThresholdCallbacks ??= new();
            _progressReachedThresholdCallbacks.Add((threshold, new CallbackEntry(callback, once)));
            return this;
        }
        public FluxObserver OnProgressReached(Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            Add(ref _progressReachedCallbacks, callback, once);
            return this;
        }
        public FluxObserver OnForceStopped(Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            Add(ref _forceStoppedCallbacks, callback, once);
            return this;
        }
        public FluxObserver OnAmountChanged(Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            Add(ref _amountChangedCallbacks, callback, once);
            return this;
        }
        public FluxObserver OnAmountReached(int threshold, Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            _amountReachedCallbacks ??= new();
            _amountReachedCallbacks.Add((threshold, new CallbackEntry(callback, once)));
            return this;
        }
        public FluxObserver OnAmountIncreased(Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            Add(ref _amountIncreasedCallbacks, callback, once);
            return this;
        }
        public FluxObserver OnAmountDecreased(Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            Add(ref _amountDecreasedCallbacks, callback, once);
            return this;
        }
        public FluxObserver OnOutOfAmount(Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            Add(ref _outOfAmountCallbacks, callback, once);
            return this;
        }
        public FluxObserver OnDisposing(Action<FluxHandle> callback, bool once = false)
        {
            if (AlreadyDisposed()) return this;
            Add(ref _disposingCallbacks, callback, once);
            return this;
        }

        public FluxObserver Where(Func<FluxHandle, bool> predicate)
        {
            _where = predicate;
            return this;
        }

        bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_flux is { IsDisposed: false })
                _flux.StateChanged -= OnStateChanged;

            _flux = null;
            _where = null;

            _progressChangedCallbacks = null;
            _progressReachedCallbacks = null;
            _forceStoppedCallbacks = null;
            _amountChangedCallbacks = null;
            _amountIncreasedCallbacks = null;
            _amountDecreasedCallbacks = null;
            _outOfAmountCallbacks = null;
            _disposingCallbacks = null;
            _amountReachedCallbacks = null;
            _progressReachedThresholdCallbacks = null;
        }
    }

    public static class FluxObserverExtensions
    {
        public static FluxObserver Observe(this FluxScope scope) => new(scope.Flux.GetFlux());
        public static FluxObserver Observe(this FluxHandle handle) => new(handle.GetFlux());
    }
}