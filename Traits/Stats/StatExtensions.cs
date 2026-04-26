using ModifiableVariable;
using ModifiableVariable.Entities;

using SackranyPawn.Components;
using SackranyPawn.Entities;

namespace SackranyPawn.Traits.Stats
{
    public static class StatExtensions
    {
        public static Modifiable<float> GetStat<T>(this Pawn pawn) where T : IStat
            => pawn.Maybe<StatHandler, Modifiable<float>>(h => h.GetStat<T>());
        public static Modifiable<float> GetStat(this Pawn pawn, IStat stat)
            => pawn.Maybe<StatHandler, Modifiable<float>>(h => h.GetStat(stat));

        public static bool TryGetStat<T>(this Pawn pawn, out Modifiable<float> value) where T : IStat
        {
            if (pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h))
                return h.TryGetStat<T>(out value);
            value = null;
            return false;
        }
        public static bool TryGetStat(this Pawn pawn, IStat stat, out Modifiable<float> value)
        {
            if (pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h))
                return h.TryGetStat(stat, out value);
            value = null;
            return false;
        }

        public static float GetStatValue<T>(this Pawn pawn, float fallback = 0f) where T : IStat
            => pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h)
                ? h.GetValue<T>(fallback)
                : fallback;
        public static float GetStatValue(this Pawn pawn, IStat stat, float fallback = 0f)
            => pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h)
                ? h.GetValue(stat, fallback)
                : fallback;

        public static bool HasStat<T>(this Pawn pawn) where T : IStat
            => pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h) && h.HasStat<T>();
        public static bool HasStat(this Pawn pawn, IStat stat)
            => pawn != null && pawn.IsActive && pawn.TryGet(out StatHandler h) && h.HasStat(stat);
        
        public static ValueChangedHandler<float> OnStatChanged<T>(this Pawn pawn, ValueChangedDelegate<float> callback) 
            where T : IStat 
            => !pawn.TryGetStat<T>(out var stat) ? default : stat.OnValueChanged(callback);
        public static ValueChangedHandler<float> OnStatChanged(this Pawn pawn, IStat stat, ValueChangedDelegate<float> callback) 
            => !pawn.TryGetStat(stat, out var s) ? default : s.OnValueChanged(callback);
    }
}