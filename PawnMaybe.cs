using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using Sackrany.Actor.Managers;

using SackranyPawn.Components;
using SackranyPawn.Entities.Modules;
using SackranyPawn.Managers;

namespace SackranyPawn
{
    public static class PawnMaybe
    {
        public static bool Maybe<TLimb>(this Pawn pawn, Action<TLimb> action)
            where TLimb : Limb
        {
            if (pawn == null || !pawn.IsActive) return false;
            if (!pawn.TryGet(out TLimb module)) return false;
            action(module);
            return true;
        }
        public static bool Maybe(this Pawn pawn, Action<Pawn> action)
        {
            if (pawn == null || !pawn.IsActive) return false;
            action(pawn);
            return true;
        }

        public static TResult Maybe<TLimb, TResult>(this Pawn pawn, Func<TLimb, TResult> func,
            TResult fallback = default)
            where TLimb : Limb
        {
            if (pawn == null || !pawn.IsActive) return fallback;
            if (!pawn.TryGet(out TLimb module)) return fallback;
            return func(module);
        }

        public static bool MaybeIf<TLimb>(this Pawn pawn, Func<TLimb, bool> predicate, Action<TLimb> action)
            where TLimb : Limb
        {
            if (pawn == null || !pawn.IsActive) return false;
            if (!pawn.TryGet(out TLimb module)) return false;
            if (!predicate(module)) return false;
            action(module);
            return true;
        }

        public static void MaybeOr<TLimb>(this Pawn pawn, Action<TLimb> action, Action fallback)
            where TLimb : Limb
        {
            if (!Maybe(pawn, action)) fallback?.Invoke();
        }
        public static void MaybeOr(this Pawn pawn, Action<Pawn> action, Action fallback)
        {
            if (!Maybe(pawn, action)) fallback?.Invoke();
        }

        public static void Command<TLimb>(this Pawn pawn, Action<TLimb> action)
            where TLimb : Limb
        {
            if (pawn == null) return;
            if (!Maybe<TLimb>(pawn, action))
                PawnLimbCommand(pawn, action, pawn.GetCancellationTokenOnDestroy()).Forget();
        }
        public static void Command(this Pawn pawn, Action<Pawn> action)
        {
            if (pawn == null) return;
            if (!Maybe(pawn, action))
                PawnCommand(pawn, action, pawn.GetCancellationTokenOnDestroy()).Forget();
        }
        public static void Command<TLimb>(this Pawn pawn, Action<TLimb> action, int timeoutMs,
            Action onTimeout = null)
            where TLimb : Limb
        {
            if (pawn == null) return;
            if (!Maybe<TLimb>(pawn, action))
                PawnLimbCommandTimeout(pawn, action, timeoutMs, onTimeout, pawn.GetCancellationTokenOnDestroy())
                    .Forget();
        }

        public static async UniTask<bool> MaybeAsync<TLimb>(this Pawn pawn, Action<TLimb> action,
            CancellationToken token = default)
            where TLimb : Limb
        {
            if (pawn == null) return false;
            if (!pawn.IsActive)
                await UniTask.WaitWhile(() => !pawn.IsActive, cancellationToken: token);
            return Maybe<TLimb>(pawn, action);
        }
        public static async UniTask<bool> MaybeAsync(this Pawn pawn, Action<Pawn> action,
            CancellationToken token = default)
        {
            if (pawn == null) return false;
            if (!pawn.IsActive)
                await UniTask.WaitWhile(() => !pawn.IsActive, cancellationToken: token);
            return Maybe(pawn, action);
        }

        public static bool MaybeFirst<TLimb>(Func<Pawn, bool> predicate, Action<TLimb> action)
            where TLimb : Limb
        {
            var pawn = PawnRegister.GetPawn(u => u.IsActive && u.Has<TLimb>() && predicate(u));
            return pawn.Maybe(action);
        }
        public static int MaybeAll<TLimb>(Func<Pawn, bool> predicate, Action<TLimb> action)
            where TLimb : Limb
        {
            int count = 0;
            foreach (var pawn in PawnRegister.GetAllPawns(
                         u => u.IsActive && u.Has<TLimb>() && predicate(u)))
                if (pawn.Maybe(action))
                    count++;
            return count;
        }

        static async UniTaskVoid PawnLimbCommand<TLimb>(Pawn pawn, Action<TLimb> action,
            CancellationToken token)
            where TLimb : Limb
        {
            await UniTask.WaitWhile(() => pawn != null && !pawn.IsActive, cancellationToken: token);
            await MaybeAsync<TLimb>(pawn, action, token);
        }
        static async UniTaskVoid PawnCommand(Pawn pawn, Action<Pawn> action, CancellationToken token)
        {
            await UniTask.WaitWhile(() => pawn != null && !pawn.IsActive, cancellationToken: token);
            await MaybeAsync(pawn, action, token);
        }
        static async UniTaskVoid PawnLimbCommandTimeout<TLimb>(
            Pawn pawn, Action<TLimb> action,
            int timeoutMs, Action onTimeout,
            CancellationToken token)
            where TLimb : Limb
        {
            var result = await UniTask
                .WaitWhile(() => pawn != null && !pawn.IsActive, cancellationToken: token)
                .TimeoutWithoutException(TimeSpan.FromMilliseconds(timeoutMs));

            if (result) onTimeout?.Invoke();
            else await MaybeAsync<TLimb>(pawn, action, token);
        }
    }
}