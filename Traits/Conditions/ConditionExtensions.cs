using SackranyPawn.Components;
using SackranyPawn.Entities;

namespace SackranyPawn.Traits.Conditions
{
    public static class ConditionExtensions
    {
        public static bool IsAllowed<T>(this Pawn pawn) where T : ICondition
            => pawn.Maybe<ConditionHandler, bool>(h => h.IsAllowed<T>(), true);

        public static bool IsBlocked<T>(this Pawn pawn) where T : ICondition
            => !pawn.IsAllowed<T>();

        public static bool Block<T>(this Pawn pawn, int amount = 1) where T : ICondition
            => pawn.Maybe<ConditionHandler>(h => h.Block<T>(amount));

        public static bool Unblock<T>(this Pawn pawn, int amount = 1) where T : ICondition
            => pawn.Maybe<ConditionHandler>(h => h.Unblock<T>(amount));

        public static bool UnblockAll<T>(this Pawn pawn) where T : ICondition
            => pawn.Maybe<ConditionHandler>(h => h.UnblockAll<T>());
    }
}