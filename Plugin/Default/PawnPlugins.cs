using SackranyPawn.Components;
using SackranyPawn.Plugin.Entities;

namespace SackranyPawn.Plugin.Default
{
    public static class PawnPlugins
    {
        /// <summary>
        /// Called in <see cref="Pawn.Awake"/> before <see cref="Pawn.Initialize"/>.
        /// Use for injection or registration before Body and Limbs are created.
        /// </summary>
        public interface IPawnAwaking : IPlugin { void Execute(Pawn pawn); }

        /// <summary>
        /// Called in <see cref="Pawn.Initialize"/> — once per object lifetime.
        /// Guaranteed: Tag, Event, Body, TimeFlow and Team are already initialized.
        /// Use for global services that need a Pawn reference immediately after creation.
        /// </summary>
        public interface IPawnInitializing : IPlugin { void Execute(Pawn pawn); }

        /// <summary>
        /// Called in <see cref="Pawn.Start"/> (Unity Start), before <see cref="Body.Start"/>.
        /// Use for setup that requires the scene to be fully loaded (other objects are already Awake).
        /// </summary>
        public interface IPawnStarting : IPlugin { void Execute(Pawn pawn); }

        /// <summary>
        /// Called every Update before iterating Limbs.
        /// <c>dt</c> is already multiplied by <see cref="Pawn.TimeFlow"/>.
        /// Use for global systems such as analytics or debug overlays.
        /// </summary>
        public interface IPawnUpdating : IPlugin { void Execute(Pawn pawn, float dt); }

        /// <summary>
        /// Called every FixedUpdate before iterating Limbs.
        /// <c>dt</c> is already multiplied by <see cref="Pawn.TimeFlow"/>.
        /// </summary>
        public interface IPawnFixedUpdating : IPlugin { void Execute(Pawn pawn, float dt); }

        /// <summary>
        /// Called every LateUpdate before iterating Limbs.
        /// <c>dt</c> is already multiplied by <see cref="Pawn.TimeFlow"/>.
        /// </summary>
        public interface IPawnLateUpdating : IPlugin { void Execute(Pawn pawn, float dt); }

        /// <summary>
        /// Called in <see cref="Pawn.StartWork"/> before registration in <see cref="Managers.PawnRegister"/>.
        /// Use to prepare state before the Pawn becomes visible to other systems.
        /// </summary>
        public interface IPawnStartWorking : IPlugin { void Execute(Pawn pawn); }

        /// <summary>
        /// Called in <see cref="Pawn.StopWork"/> before unregistration from <see cref="Managers.PawnRegister"/>.
        /// Use to clean up subscriptions or effects while the Pawn is still formally active.
        /// </summary>
        public interface IPawnStopWorking : IPlugin { void Execute(Pawn pawn); }

        /// <summary>
        /// Called in <see cref="Pawn.ResetPawn"/> before Tag, Event, TimeFlow and Body are reset.
        /// Use to save or log state before the reset occurs.
        /// </summary>
        public interface IPawnResetting : IPlugin { void Execute(Pawn pawn); }

        /// <summary>
        /// Called in <see cref="Pawn.OnPopped"/> — Pawn has been retrieved from the pool,
        /// before <see cref="Pawn.ResetPawn"/> and <see cref="Pawn.StartWork"/>.
        /// Use to reconfigure pool-recycled instances before they become active.
        /// </summary>
        public interface IPawnPopping : IPlugin { void Execute(Pawn pawn); }

        /// <summary>
        /// Called in <see cref="Pawn.OnPushed"/> — Pawn is being returned to the pool,
        /// before <see cref="Pawn.StopWork"/> and <c>gameObject.SetActive(false)</c>.
        /// Use to snapshot or release resources tied to this pool cycle.
        /// </summary>
        public interface IPawnPushing : IPlugin { void Execute(Pawn pawn); }

        /// <summary>
        /// Called in <see cref="Pawn.OnDestroy"/> before <see cref="Body.Dispose"/>.
        /// Not called when the application is quitting.
        /// Use for global cleanup such as unregistering from external systems.
        /// </summary>
        public interface IPawnDestroying : IPlugin { void Execute(Pawn pawn); }
    }
}