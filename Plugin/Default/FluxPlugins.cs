using SackranyPawn.Plugin.Entities;
using SackranyPawn.Traits.Fluxes.Entities;

namespace SackranyPawn.Plugin.Default
{
    public static class FluxPlugins
    {
        /// <summary>
        /// Called in <see cref="Flux.Start"/> before <see cref="Flux.OnStart"/>.
        /// Guaranteed: <see cref="Flux.Handler"/>, <see cref="Flux.Token"/> and
        /// reactive properties are already initialized via <see cref="Flux.Initialize"/>.
        /// Use for tracing, profiling or binding external systems to the Flux.
        /// </summary>
        public interface IFluxStarting : IPlugin { void Execute(FluxHandle flux); }

        /// <summary>
        /// Called in <see cref="Flux.ForceStop"/> before <see cref="Flux.OnForceStop"/>
        /// and before the <see cref="FluxState.ForceStopped"/> state change fires.
        /// <see cref="Flux.IsStarted"/> is still <c>true</c> at call time.
        /// Use to react to external cancellation before the Flux tears down.
        /// </summary>
        public interface IFluxForceStopping : IPlugin { void Execute(FluxHandle flux); }

        /// <summary>
        /// Called in <see cref="Flux.Dispose"/> before <see cref="Flux.OnDisposing"/>
        /// and before the CancellationToken is cancelled.
        /// Only fires on the normal disposal path — not when the Flux was never started.
        /// Use to release external references before the Flux cleans itself up.
        /// </summary>
        public interface IFluxDisposing : IPlugin { void Execute(FluxHandle flux); }
    }
}