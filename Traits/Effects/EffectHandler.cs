using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using SackranyPawn.Entities.Modules;
using SackranyPawn.Entities.Modules.ModuleComposition;
using SackranyPawn.Traits.Effects.Static;

using UnityEngine;

namespace SackranyPawn.Traits.Effects
{
    [Serializable]
    public class EffectHandler : AsyncLimb, IUpdateLimb, IFixedUpdateLimb
    {
        [SerializeField][SerializeReference][SubclassSelector] 
        public Effect[] Default;
        
        readonly Dictionary<int, Effect> _effects = new ();
        readonly Dictionary<int, List<(UniTask task, CancellationTokenSource cts)>> _effectTasks = new ();
        
        public float deltaTime { get; private set; }
        public float fixedDeltaTime { get; private set; }
        
        protected override void OnStart()
        {
            ApplyEffects(Default);
        }

        #region Effect
        public bool ApplyEffects(Effect[] effects)
        {
            bool allApplied = true;
            foreach (var effect in effects)
            {
                allApplied &= ApplyEffect(effect);
            }
            return allApplied;
        }
        
        public bool ApplyEffect<T>(int amount = 1) where T : Effect, new ()
            => ApplyEffect(new T(), amount);
        public bool ApplyEffect(Effect effect, int amount = 1)
        {
            if (!Pawn.IsActive) return false;
            if (_effects.TryGetValue(effect.Id, out var e))
            {
                e.IncreaseAmount(amount);
                return true;
            }
            
            _effects.Add(effect.Id, effect);
            effect.Initialize(this, amount);
            return true;
        }
        
        public bool RemoveEffect<T>() where T : Effect
        {
            if (!Pawn.IsActive) return false;
            if (!_effects.TryGetValue(EffectRegistry.GetId<T>(), out var e))
                return false;
            CancelAllEffectTasks<T>();
            _effects.Remove(EffectRegistry.GetId<T>());
            e.Dispose();
            return true;
        }
        public bool RemoveEffect(Effect effect)
        {
            if (!Pawn.IsActive) return false;
            if (!_effects.TryGetValue(effect.Id, out var e))
                return false;
            CancelAllEffectTasks(effect);
            _effects.Remove(effect.Id);
            e.Dispose();
            return true;
        }
        public bool RemoveAllEffects()
        {
            if (!Pawn.IsActive) return false;
            foreach (var effect in _effects.Values)
            {
                CancelAllEffectTasks(effect);
                effect.Dispose();
            }
            _effects.Clear();
            return true;
        }

        public bool ChangeEffectAmount<T>(int offset) where T : Effect
        {
            if (!Pawn.IsActive) return false;
            if (!_effects.TryGetValue(EffectRegistry.GetId<T>(), out var e))
                return false;
            switch (offset)
            {
                case 0:
                    return true;
                case > 0:
                    e.IncreaseAmount(offset);
                    break;
                case < 0:
                    e.DecreaseAmount(offset);
                    break;
            }
            return true;
        }
        public bool ChangeEffectAmount<T>(T effect, int offset) where T : Effect
        {
            if (!Pawn.IsActive) return false;
            if (!_effects.TryGetValue(effect.Id, out var e))
                return false;
            switch (offset)
            {
                case 0:
                    return true;
                case > 0:
                    e.IncreaseAmount(offset);
                    break;
                case < 0:
                    e.DecreaseAmount(offset);
                    break;
            }
            return true;
        }
        #endregion

        #region Tasks
        public CancellationTokenSource StartEffectTask(Effect effect, Func<CancellationToken, UniTask> taskFactory)
        {
            if (!Pawn.IsActive) return null;
            
            var cts = new CancellationTokenSource();
            var trackedTask = TrackTaskCompletion(effect.Id, taskFactory, cts);
            trackedTask.Forget();

            if (!_effectTasks.TryGetValue(effect.Id, out var tasks))
            {
                tasks = new ();
                _effectTasks.Add(effect.Id, tasks);
            }
            tasks.Add((trackedTask, cts));
            return cts;
        }
        public CancellationTokenSource StartEffectTask<T>(Func<CancellationToken, UniTask> taskFactory) where T : Effect
        {
            if (!Pawn.IsActive) return null;

            var id = EffectRegistry.GetId<T>();
            var cts = new CancellationTokenSource();
            var trackedTask = TrackTaskCompletion(id, taskFactory, cts);
            trackedTask.Forget();

            if (!_effectTasks.TryGetValue(id, out var tasks))
            {
                tasks = new ();
                _effectTasks.Add(id, tasks);
            }
            tasks.Add((trackedTask, cts));
            return cts;
        }

        async UniTask TrackTaskCompletion(
            int effectId, 
            Func<CancellationToken, UniTask> taskFactory, 
            CancellationTokenSource cts)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            try
            {
                await taskFactory(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (_effectTasks.TryGetValue(effectId, out var tasks))
                {
                    for (int i = tasks.Count - 1; i >= 0; i--)
                        if (tasks[i].cts == cts) tasks.RemoveAt(i);
                    if (tasks.Count == 0) _effectTasks.Remove(effectId);
                }
                cts?.Dispose();
            }
        }

        public void CancelAllEffectTasks(Effect effect)
        {
            if (!Pawn.IsActive) return;
            if (_effectTasks.TryGetValue(effect.Id, out var tasks))
            {
                for (int i = tasks.Count - 1; i >= 0; i--)
                {
                    var task = tasks[i];
                    if (task.cts != null && !task.cts.IsCancellationRequested) 
                        task.cts.Cancel();
                }
            }
        }
        public void CancelAllEffectTasks<T>() where T : Effect
        {
            if (!Pawn.IsActive) return;
            var id = EffectRegistry.GetId<T>();
            if (_effectTasks.TryGetValue(id, out var tasks))
            {
                for (int i = tasks.Count - 1; i >= 0; i--)
                {
                    var task = tasks[i];
                    if (task.cts != null && !task.cts.IsCancellationRequested) 
                        task.cts.Cancel();
                }
            }
        }

        public void CancelAllTasks()
        {
            if (!Pawn.IsActive) return;
    
            var allTasksLists = _effectTasks.Values.ToArray();
            foreach (var taskList in allTasksLists)
            {
                var tasksCopy = taskList.ToArray();
                foreach (var value in tasksCopy)
                {
                    if (value.cts != null && !value.cts.IsCancellationRequested)
                    {
                        value.cts.Cancel();
                    }
                }
            }
        }
        #endregion

        #region Get
        public T GetEffect<T>() where T : Effect
        {
            if (!_effects.TryGetValue(EffectRegistry.GetId<T>(), out var e) || e.IsDisposed)
                return null;
            return e as T;
        }
        public bool TryGetEffect<T>(out T effect) where T : Effect
        {
            if (!_effects.TryGetValue(EffectRegistry.GetId<T>(), out var e) || e.IsDisposed)
            {
                effect = null;
                return false;
            }
            effect = e as T;
            return true;
        }
        public bool HasEffect<T>() where T : Effect
        {
            return _effects.TryGetValue(EffectRegistry.GetId<T>(), out var e) && !e.IsDisposed;
        }
        #endregion
        
        protected override void OnDispose()
        {
            RemoveAllEffects();
        }
        protected override void OnReset()
        {
            RemoveAllEffects();
        }
        
        public void OnUpdate(float dt)
        {
            deltaTime = dt;
        }
        public void OnFixedUpdate(float dt)
        {
            fixedDeltaTime = dt;
        }
    }
}