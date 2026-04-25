using System;
using System.Threading;

using Cysharp.Threading.Tasks;
using SackranyPawn.Traits.Effects.Static;

using UnityEngine;

namespace SackranyPawn.Traits.Effects
{
    [Serializable]
    public abstract class Effect : IDisposable
    {
        public abstract int Id { get; }
        EffectHandler Controller;

        public bool IsDisposed { get; private set; }
        public int Amount { get; private set; }

        private protected float deltaTime => Controller.deltaTime;
        private protected float fixedDeltaTime => Controller.fixedDeltaTime;

        public void Initialize(EffectHandler controller, int amount = 1)
        {
            if (IsDisposed) return;
            Controller = controller;
            Amount = amount;
        }

        public void IncreaseAmount(int diff)
        {
            if (IsDisposed) return;
            diff = Mathf.Abs(diff);
            Amount += diff;
            OnAmountIncreased(diff);
            OnAmountChanged(diff);
        }
        public void DecreaseAmount(int diff)
        {
            if (IsDisposed) return;
            diff = Mathf.Abs(diff);
            diff = Mathf.Min(Amount, diff);
            Amount -= diff;
            OnAmountDecreased(diff);
            OnAmountChanged(-diff);

            if (Amount <= 0)
            {
                OnOutOfAmount();
                Controller.RemoveEffect(this);
            }
        }
        protected virtual void OnAmountIncreased(int offset) { }
        protected virtual void OnAmountDecreased(int offset) { }
        protected virtual void OnAmountChanged(int offset) { }
        protected virtual void OnOutOfAmount() { }

        public void Reset()
        {
            if (IsDisposed) return;
            Amount = 1;
            OnReset();
        }
        protected virtual void OnReset() { }

        #region Tasks
        private protected CancellationTokenSource StartTask(
            Func<CancellationToken, UniTask> taskFactory,
            bool autoRemove = false)
            => Controller.StartEffectTask(this, async token =>
            {
                try
                {
                    await taskFactory(token);
                }
                finally
                {
                    if (autoRemove && !token.IsCancellationRequested) Controller.RemoveEffect(this);
                }
            });

        private protected CancellationTokenSource StartTask<T1>(
            T1 v1,
            Func<T1, CancellationToken, UniTask> taskFactory,
            bool autoRemove = false)
            => Controller.StartEffectTask(this, async token =>
            {
                try
                {
                    await taskFactory(v1, token);
                }
                finally
                {
                    if (autoRemove && !token.IsCancellationRequested) Controller.RemoveEffect(this);
                }
            });

        private protected CancellationTokenSource StartTask<T1, T2>(
            T1 v1, T2 v2,
            Func<T1, T2, CancellationToken, UniTask> taskFactory,
            bool autoRemove = false)
            => Controller.StartEffectTask(this, async token =>
            {
                try
                {
                    await taskFactory(v1, v2, token);
                }
                finally
                {
                    if (autoRemove && !token.IsCancellationRequested) Controller.RemoveEffect(this);
                }
            });

        private protected CancellationTokenSource StartTask<T1, T2, T3>(
            T1 v1, T2 v2, T3 v3,
            Func<T1, T2, T3, CancellationToken, UniTask> taskFactory,
            bool autoRemove = false)
            => Controller.StartEffectTask(this, async token =>
            {
                try
                {
                    await taskFactory(v1, v2, v3, token);
                }
                finally
                {
                    if (autoRemove && !token.IsCancellationRequested) Controller.RemoveEffect(this);
                }
            });

        private protected CancellationTokenSource StartTask<T1, T2, T3, T4>(
            T1 v1, T2 v2, T3 v3, T4 v4,
            Func<T1, T2, T3, T4, CancellationToken, UniTask> taskFactory,
            bool autoRemove = false)
            => Controller.StartEffectTask(this, async token =>
            {
                try
                {
                    await taskFactory(v1, v2, v3, v4, token);
                }
                finally
                {
                    if (autoRemove && !token.IsCancellationRequested) Controller.RemoveEffect(this);
                }
            });

        private protected CancellationTokenSource StartTask<T1, T2, T3, T4, T5>(
            T1 v1, T2 v2, T3 v3, T4 v4, T5 v5,
            Func<T1, T2, T3, T4, T5, CancellationToken, UniTask> taskFactory,
            bool autoRemove = false)
            => Controller.StartEffectTask(this, async token =>
            {
                try
                {
                    await taskFactory(v1, v2, v3, v4, v5, token);
                }
                finally
                {
                    if (autoRemove && !token.IsCancellationRequested) Controller.RemoveEffect(this);
                }
            });

        private protected CancellationTokenSource StartTickingTask(
            TimeSpan interval,
            Func<CancellationToken, UniTask> action,
            DelayType delayType = DelayType.DeltaTime,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            bool autoRemove = true)
            => StartTask(async token =>
            {
                while (!token.IsCancellationRequested)
                {
                    await action(token);
                    await UniTask.Delay(interval, delayType, timing, cancellationToken: token);
                }
            }, autoRemove);

        private protected CancellationTokenSource StartTickingTask<T1>(
            T1 v1,
            TimeSpan interval,
            Func<T1, CancellationToken, UniTask> action,
            DelayType delayType = DelayType.DeltaTime,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            bool autoRemove = true)
            => StartTask(v1, async (l1, token) =>
            {
                while (!token.IsCancellationRequested)
                {
                    await action(l1, token);
                    await UniTask.Delay(interval, delayType, timing, cancellationToken: token);
                }
            }, autoRemove);

        private protected CancellationTokenSource StartTickingTask<T1, T2>(
            T1 v1, T2 v2,
            TimeSpan interval,
            Func<T1, T2, CancellationToken, UniTask> action,
            DelayType delayType = DelayType.DeltaTime,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            bool autoRemove = true)
            => StartTask(v1, v2, async (l1, l2, token) =>
            {
                while (!token.IsCancellationRequested)
                {
                    await action(l1, l2, token);
                    await UniTask.Delay(interval, delayType, timing, cancellationToken: token);
                }
            }, autoRemove);

        private protected CancellationTokenSource StartTickingTask<T1, T2, T3>(
            T1 v1, T2 v2, T3 v3,
            TimeSpan interval,
            Func<T1, T2, T3, CancellationToken, UniTask> action,
            DelayType delayType = DelayType.DeltaTime,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            bool autoRemove = true)
            => StartTask(v1, v2, v3, async (l1, l2, l3, token) =>
            {
                while (!token.IsCancellationRequested)
                {
                    await action(l1, l2, l3, token);
                    await UniTask.Delay(interval, delayType, timing, cancellationToken: token);
                }
            }, autoRemove);

        private protected CancellationTokenSource StartTickingTask<T1, T2, T3, T4>(
            T1 v1, T2 v2, T3 v3, T4 v4,
            TimeSpan interval,
            Func<T1, T2, T3, T4, CancellationToken, UniTask> action,
            DelayType delayType = DelayType.DeltaTime,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            bool autoRemove = true)
            => StartTask(v1, v2, v3, v4, async (l1, l2, l3, l4, token) =>
            {
                while (!token.IsCancellationRequested)
                {
                    await action(l1, l2, l3, l4, token);
                    await UniTask.Delay(interval, delayType, timing, cancellationToken: token);
                }
            }, autoRemove);

        private protected CancellationTokenSource StartTickingTask<T1, T2, T3, T4, T5>(
            T1 v1, T2 v2, T3 v3, T4 v4, T5 v5,
            TimeSpan interval,
            Func<T1, T2, T3, T4, T5, CancellationToken, UniTask> action,
            DelayType delayType = DelayType.DeltaTime,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            bool autoRemove = true)
            => StartTask(v1, v2, v3, v4, v5, async (l1, l2, l3, l4, l5, token) =>
            {
                while (!token.IsCancellationRequested)
                {
                    await action(l1, l2, l3, l4, l5, token);
                    await UniTask.Delay(interval, delayType, timing, cancellationToken: token);
                }
            }, autoRemove);

        private protected void CancelAllTasks() => Controller.CancelAllEffectTasks(this);
        #endregion

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
        }
    }

    [Serializable]
    public abstract class Effect<TSelf> : Effect 
        where TSelf : Effect<TSelf>
    {
        public sealed override int Id => EffectRegistry.GetId<TSelf>();
    }
    
    public interface EffectTemplate
    {
        public int GetId();
        public Effect GetInstance();
    }
    public interface EffectTemplate<out T> : EffectTemplate
        where T : Effect, new ()
    {
        int EffectTemplate.GetId() => EffectRegistry.GetId<T>();
        Effect EffectTemplate.GetInstance() => new T();
    }
}