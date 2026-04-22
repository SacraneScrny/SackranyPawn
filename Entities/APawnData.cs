using SackranyPawn.Components;

namespace SackranyPawn.Entities
{
    public abstract class APawnData
    {
        private protected Pawn _pawn;
        public void Initialize(Pawn pawn)
        {
            _pawn = pawn;
            OnInitialize();
        }
        private protected virtual void OnInitialize() { }
        
        public abstract void Reset();
    }
}