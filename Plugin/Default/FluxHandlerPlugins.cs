using SackranyPawn.Plugin.Entities;
using SackranyPawn.Traits.Fluxes;
using SackranyPawn.Traits.Fluxes.Entities;

namespace SackranyPawn.Plugin.Default
{
    public static class FluxHandlerPlugins
    {
        /// <summary>
        /// Called in <see cref="FluxHandler.ApplyFlux"/> after <see cref="Flux.Start"/>
        /// and after the <see cref="FluxHandler.FluxAdded"/> event.
        /// Use for cross-cutting concerns such as UI binding or analytics on Flux activation.
        /// </summary>
        public interface IFluxHandlerFluxApplied : IPlugin { void Execute(FluxHandler handler, FluxHandle flux); }

        /// <summary>
        /// Called in <see cref="FluxHandler.RemoveInternal"/> after the
        /// <see cref="FluxHandler.FluxRemoved"/> event and before <see cref="Flux.Dispose"/>.
        /// Use to release external references to the Flux before it is destroyed.
        /// </summary>
        public interface IFluxHandlerFluxRemoved : IPlugin { void Execute(FluxHandler handler, FluxHandle flux); }

        /// <summary>
        /// Called in <see cref="FluxHandler.OnReset"/> before all Fluxes are disposed
        /// and the internal state is cleared.
        /// Use to snapshot or flush caches tied to the current handler cycle.
        /// </summary>
        public interface IFluxHandlerResetting : IPlugin { void Execute(FluxHandler handler); }

        /// <summary>
        /// Called in <see cref="FluxHandler.OnDispose"/> before
        /// <see cref="FluxHandler.RemoveAllFluxes"/>.
        /// Guaranteed to run exactly once per FluxHandler lifetime.
        /// Use to unsubscribe from events or release resources before all Fluxes are torn down.
        /// </summary>
        public interface IFluxHandlerDisposing : IPlugin { void Execute(FluxHandler handler); }
    }
}