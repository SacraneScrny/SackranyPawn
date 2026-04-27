using SackranyPawn.Components;
using SackranyPawn.Plugin.Entities;

namespace SackranyPawn.Plugin.Default
{
    public static class PawnPlugins
    {
        public interface IPawnStartWorking : IPlugin { public void Execute(Pawn pawn); }        
        public interface IPawnStopWorking : IPlugin { public void Execute(Pawn pawn); }
        public interface IPawnResetting : IPlugin { public void Execute(Pawn pawn); }        
        public interface IPawnPopping : IPlugin { public void Execute(Pawn pawn); }
        public interface IPawnPushing : IPlugin { public void Execute(Pawn pawn); }
        public interface IPawnDestroying : IPlugin { public void Execute(Pawn pawn); }
        
        public interface IPawnAwaking : IPlugin { public void Execute(Pawn pawn); }
        public interface IPawnInitializing : IPlugin { public void Execute(Pawn pawn); }
        public interface IPawnStarting : IPlugin { public void Execute(Pawn pawn); }
        public interface IPawnUpdating : IPlugin { public void Execute(Pawn pawn, float dt); }
        public interface IPawnFixedUpdating : IPlugin { public void Execute(Pawn pawn, float dt); }
        public interface IPawnLateUpdating : IPlugin { public void Execute(Pawn pawn, float dt); }
    }
}