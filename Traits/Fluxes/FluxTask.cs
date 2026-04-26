using System;

using Cysharp.Threading.Tasks;

using SackranyPawn.Traits.Fluxes.Entities;

namespace SackranyPawn.Traits.Fluxes
{
    public static class FluxTask
    {
        public static void Periodic(Flux flux, float interval, Action onTick)
        {
            flux.StartTask(async token =>
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.WaitForSeconds(interval, cancellationToken: token);
                    onTick();
                }
            });
        }

        public static void Conditional(Flux flux, Func<bool> condition, Action onExpired)
        {
            flux.StartTask(async token =>
            {
                await UniTask.WaitUntil(() => !condition(), cancellationToken: token);
                onExpired();
            });
        }
    }
}