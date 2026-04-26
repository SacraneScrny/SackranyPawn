using SackranyPawn.Components;
using SackranyPawn.Entities;

namespace SackranyPawn.Traits.Conditions
{
    public static class ConditionExtensions
    {
        public static bool IsAllowed<T>(this Pawn pawn) where T : ICondition
            => pawn.Maybe<ConditionHandler, bool>(h => h.IsAllowed<T>(), true);
        public static bool IsAllowed(this Pawn pawn, ICondition condition)
            => pawn.Maybe<ConditionHandler, bool>(h => h.IsAllowed(condition), true);

        public static bool IsBlocked<T>(this Pawn pawn) where T : ICondition
            => !pawn.IsAllowed<T>();
        public static bool IsBlocked(this Pawn pawn, ICondition condition)
            => !pawn.IsAllowed(condition);

        public static bool Block<T>(this Pawn pawn, int amount = 1) where T : ICondition
            => pawn.Maybe<ConditionHandler>(h => h.Block<T>(amount));
        public static bool Block(this Pawn pawn, ICondition condition, int amount = 1)
            => pawn.Maybe<ConditionHandler>(h => h.Block(condition, amount));

        public static bool Unblock<T>(this Pawn pawn, int amount = 1) where T : ICondition
            => pawn.Maybe<ConditionHandler>(h => h.Unblock<T>(amount));
        public static bool Unblock(this Pawn pawn, ICondition condition, int amount = 1)
            => pawn.Maybe<ConditionHandler>(h => h.Unblock(condition, amount));

        public static bool UnblockAll<T>(this Pawn pawn) where T : ICondition
            => pawn.Maybe<ConditionHandler>(h => h.UnblockAll<T>());
        public static bool UnblockAll(this Pawn pawn, ICondition condition)
            => pawn.Maybe<ConditionHandler>(h => h.UnblockAll(condition));
    }
}