using SackranyPawn.Entities.Modules;
using SackranyPawn.Plugin.Entities;

namespace SackranyPawn.Plugin.Default
{
    public static class LimbPlugins
    {
        /// <summary>
        /// Called in <see cref="Limb.Awake"/> before <see cref="Limb.OnAwakeInternal"/>
        /// and <see cref="Limb.OnAwake"/>.
        /// Guaranteed: Pawn and Body references are already set via
        /// <see cref="Limb.FillPawn"/> and <see cref="Limb.FillBody"/>.
        /// Dependencies are NOT yet injected at this point — use <see cref="ILimbStarting"/> for that.
        /// Use for one-time allocation or registration that must happen before Start.
        /// </summary>
        public interface ILimbAwaking : IPlugin { void Execute(Limb limb); }

        /// <summary>
        /// Called in <see cref="Limb.Start"/> before <see cref="Limb.OnStartInternal"/>
        /// and <see cref="Limb.OnStart"/>.
        /// Guaranteed: all <see cref="DependencyAttribute"/> fields are injected,
        /// <see cref="Limb.OnDependencyCheck"/> has passed.
        /// Use for tracing, profiling hooks or binding external systems to the Limb.
        /// </summary>
        public interface ILimbStarting : IPlugin { void Execute(Limb limb); }

        /// <summary>
        /// Called in <see cref="Limb.Reset"/> before <see cref="Limb.OnResetInternal"/>
        /// and <see cref="Limb.OnReset"/>.
        /// Note: <see cref="Limb.IsStarted"/> is still <c>true</c> at call time.
        /// Use to flush per-cycle state in external systems before the Limb resets itself.
        /// </summary>
        public interface ILimbResetting : IPlugin { void Execute(Limb limb); }

        /// <summary>
        /// Called in <see cref="Limb.Dispose"/> before <see cref="Limb.OnDisposeInternal"/>
        /// and <see cref="Limb.OnDispose"/>.
        /// Only fires on the normal disposal path (after Awake). The pre-awake disposal
        /// path (<see cref="Limb.OnDisposeBeforeAwaken"/>) does not trigger this plugin
        /// because Pawn and Body are not guaranteed to be set in that case.
        /// Use to unregister the Limb from external systems before it tears itself down.
        /// </summary>
        public interface ILimbDisposing : IPlugin { void Execute(Limb limb); }
    }
}