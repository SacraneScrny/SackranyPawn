using System;

namespace SackranyPawn.Traits.Fluxes.Entities
{
    public readonly struct FluxScope : IDisposable
    {
        public FluxHandle Flux => _flux;
        readonly Flux _flux;
        readonly Func<Flux, bool> _disposePredicate;
        
        public FluxScope(Flux flux, Func<Flux, bool> disposePredicate)
        {
            _flux = flux;
            _disposePredicate = disposePredicate;
        }
        
        public void Dispose()
        {
            if (_disposePredicate == null) return;
            if (Flux == null || Flux.IsDisposed) return;
            _disposePredicate(_flux);
        }
    }
}