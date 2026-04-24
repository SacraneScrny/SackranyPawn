using ModifiableVariable;
using ModifiableVariable.Entities;

using SackranyPawn.Components;

namespace SackranyPawn.Traits.Stats
{
    public static class StatExtensions
    {
        public static Modifiable<float> GetStat<T>(this Pawn pawn) where T : IStat
            => pawn.Maybe<StatHandler, Modifiable<float>>(h => h.GetStat<T>());

        public static bool TryGetStat<T>(this Pawn pawn, out Modifiable<float> value) where T : IStat
        {
            if (pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h))
                return h.TryGetStat<T>(out value);
            value = null;
            return false;
        }

        public static float GetStatValue<T>(this Pawn pawn, float fallback = 0f) where T : IStat
            => pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h)
                ? h.GetValue<T>(fallback)
                : fallback;

        public static bool HasStat<T>(this Pawn pawn) where T : IStat
            => pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h) && h.HasStat<T>();
        
        
        public static ValueChangedHandler<float> OnStatChanged<T>(this Pawn pawn, ValueChangedDelegate<float> callback) 
            where T : IStat 
            => !pawn.TryGetStat<T>(out var stat) ? default : stat.OnValueChanged(callback);
    }
}