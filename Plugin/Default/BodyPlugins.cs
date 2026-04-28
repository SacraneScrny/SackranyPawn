using SackranyPawn.Entities.Modules;
using SackranyPawn.Plugin.Entities;

namespace SackranyPawn.Plugin.Default
{
    public static class BodyPlugins
    {
        /// <summary>
        /// Called in <see cref="Body.Start"/> before the serialized Limb array is added.
        /// Guaranteed: Pawn is initialized, Body mode is set.
        /// Use to inject additional Limbs or configure the Body before it becomes active.
        /// </summary>
        public interface IBodyStarting : IPlugin { void Execute(Body body); }

        /// <summary>
        /// Called in <see cref="Body.ActivateModule"/> after <see cref="Limb.Start"/>
        /// and after the <see cref="Body.LimbAdded"/> event.
        /// Use for cross-cutting concerns such as tracing, profiling or binding UI.
        /// </summary>
        public interface IBodyLimbAdded : IPlugin { void Execute(Body body, Limb limb); }

        /// <summary>
        /// Called in <see cref="Body.RemoveSingle"/> after the <see cref="Body.LimbRemoved"/>
        /// event and before <see cref="Limb.Dispose"/>.
        /// Use to release external references to the Limb before it is destroyed.
        /// </summary>
        public interface IBodyLimbRemoved : IPlugin { void Execute(Body body, Limb limb); }

        /// <summary>
        /// Called in <see cref="Body.Reset"/> before temporary Limbs are removed
        /// and before persistent Limbs are reset.
        /// Use to snapshot runtime state or flush caches tied to the current Body cycle.
        /// </summary>
        public interface IBodyResetting : IPlugin { void Execute(Body body); }

        /// <summary>
        /// Called in <see cref="Body.Dispose"/> before any Limb is disposed.
        /// Guaranteed to run exactly once per Body lifetime.
        /// Use to unsubscribe from events or release unmanaged resources.
        /// </summary>
        public interface IBodyDisposing : IPlugin { void Execute(Body body); }
    }
}