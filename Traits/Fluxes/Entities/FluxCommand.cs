using System;
using System.Collections.Generic;

namespace SackranyPawn.Traits.Fluxes.Entities
{
    public sealed class FluxCommand : IDisposable
    {
        Flux _flux;
        List<Action<FluxHandle>> _startedCallbacks;
        List<Action<FluxHandle>> _progressReachedCallbacks;
        List<Action<FluxHandle>> _forceStoppedCallbacks;
        List<Action<FluxHandle>> _amountChangedCallbacks;
        List<Action<FluxHandle>> _amountIncreasedCallbacks;
        List<Action<FluxHandle>> _amountDecreasedCallbacks;
        List<Action<FluxHandle>> _outOfAmountCallbacks;

        public FluxCommand(Flux flux)
        {
            _flux = flux;
            _flux.StateChanged += FluxOnStateChanged;
        }
        
        void FluxOnStateChanged(Flux f, FluxState state)
        {
            if (_disposed) return;
            
            switch (state)
            {
                case FluxState.Started: Call(_startedCallbacks); break;
                case FluxState.ProgressReached: Call(_progressReachedCallbacks); break;
                case FluxState.ForceStopped: Call(_forceStoppedCallbacks); break;
                case FluxState.AmountChanged: Call(_amountChangedCallbacks); break;
                case FluxState.AmountIncreased: Call(_amountIncreasedCallbacks); break;
                case FluxState.AmountDecreased: Call(_amountDecreasedCallbacks); break;
                case FluxState.OutOfAmount: Call(_outOfAmountCallbacks); break;
                
                case FluxState.Disposing: Dispose(); break;
                
                case FluxState.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
        void Call(List<Action<FluxHandle>> callbacks)
        {
            for (int i = 0; i < callbacks.Count; i++)
                callbacks[i](_flux);
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
        
        public FluxCommand OnStarted(Action<FluxHandle> callback)
        {
            if (AlreadyDisposed()) return this;
            _startedCallbacks ??= new ();
            _startedCallbacks.Add(callback);
            return this;
        }
        public FluxCommand OnProgressReached(Action<FluxHandle> callback)
        {
            if (AlreadyDisposed()) return this;
            _progressReachedCallbacks ??= new ();
            _progressReachedCallbacks.Add(callback);
            return this;
        }
        public FluxCommand OnForceStopped(Action<FluxHandle> callback)
        {
            if (AlreadyDisposed()) return this;
            _forceStoppedCallbacks ??= new ();
            _forceStoppedCallbacks.Add(callback);
            return this;
        }
        public FluxCommand OnAmountChanged(Action<FluxHandle> callback)
        {
            if (AlreadyDisposed()) return this;
            _amountChangedCallbacks ??= new ();
            _amountChangedCallbacks.Add(callback);
            return this;
        }
        public FluxCommand OnAmountIncreased(Action<FluxHandle> callback)
        {
            if (AlreadyDisposed()) return this;
            _amountIncreasedCallbacks ??= new ();
            _amountIncreasedCallbacks.Add(callback);
            return this;
        }        
        public FluxCommand OnAmountDecreased(Action<FluxHandle> callback)
        {
            if (AlreadyDisposed()) return this;
            _amountDecreasedCallbacks ??= new ();
            _amountDecreasedCallbacks.Add(callback);
            return this;
        }
        public FluxCommand OnOutOfAmount(Action<FluxHandle> callback)
        {
            if (AlreadyDisposed()) return this;
            _outOfAmountCallbacks ??= new ();
            _outOfAmountCallbacks.Add(callback);
            return this;
        }

        bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_flux is { IsDisposed: false })
            {
                _flux.StateChanged -= FluxOnStateChanged;
            }
            
            _flux = null;
            
            _startedCallbacks?.Clear();
            _startedCallbacks = null;
            
            _progressReachedCallbacks?.Clear();
            _progressReachedCallbacks = null;
            
            _forceStoppedCallbacks?.Clear();
            _forceStoppedCallbacks = null;
            
            _amountChangedCallbacks?.Clear();
            _amountChangedCallbacks = null;
            
            _amountIncreasedCallbacks?.Clear();
            _amountIncreasedCallbacks = null;
            
            _amountDecreasedCallbacks?.Clear();
            _amountDecreasedCallbacks = null;
            
            _outOfAmountCallbacks?.Clear();
            _outOfAmountCallbacks = null;
        }
    }

    public static class FluxCommandExtensions
    {
        public static FluxCommand AsCommand(this FluxScope scope) 
            => new(scope.Flux.GetFlux());
        public static FluxCommand AsCommand(this FluxHandle handle) 
            => new(handle.GetFlux());
        public static FluxCommand AsCommand(this Flux flux) 
            => new(flux);
    }
}