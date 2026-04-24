using SackranyPawn.Components;
using SackranyPawn.Entities;

namespace SackranyPawn.Traits.Effects
{
    public static class EffectExtensions
    {        
        public static bool ApplyEffects(this Pawn pawn, EffectTemplate[] effects)
            => pawn.Maybe<EffectHandler>(h => h.ApplyEffects(effects));
        public static bool ApplyEffect<T>(this Pawn pawn, int amount = 1) where T : EffectTemplate, new ()
            => pawn.Maybe<EffectHandler>(h => h.ApplyEffect<T>(amount));
        public static bool ApplyEffect<T>(this Pawn pawn, T effect, int amount = 1) where T : EffectTemplate
            => pawn.Maybe<EffectHandler>(h => h.ApplyEffect(effect, amount));

        public static bool RemoveEffect<T>(this Pawn pawn) where T : Effect
            => pawn.Maybe<EffectHandler>(h => h.RemoveEffect<T>());
        public static bool RemoveEffect<T>(this Pawn pawn, T effect) where T : Effect 
            => pawn.Maybe<EffectHandler>(h => h.RemoveEffect<T>(effect));
        public static bool RemoveAllEffects(this Pawn pawn)
            => pawn.Maybe<EffectHandler>(h => h.RemoveAllEffects());

        public static bool ChangeEffectAmount<T>(this Pawn pawn, int offset) where T : Effect
            => pawn.Maybe<EffectHandler>(h => h.ChangeEffectAmount<T>(offset));
        public static bool ChangeEffectAmount<T>(this Pawn pawn, T effect, int offset) where T : Effect 
            => pawn.Maybe<EffectHandler>(h => h.ChangeEffectAmount<T>(effect, offset));
    }
}