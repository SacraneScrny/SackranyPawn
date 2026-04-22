using SackranyPawn.Components;
using SackranyPawn.Entities.Modules;

namespace SackranyPawn.Entities
{
    public abstract class PawnBase
    {
        public Pawn Pawn { get; private set; }
        protected Body Body;
        
        public bool HasPawn => Pawn != null;
        public bool HasBody => Body != null;
        
        public void FillPawn(Pawn pawn) => Pawn = pawn;
        public void FillBody(Body body) => Body = body;
    }
}