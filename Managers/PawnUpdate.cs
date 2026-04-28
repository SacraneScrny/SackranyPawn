using System.Collections.Generic;

using SackranyPawn.Components;
using SackranyPawn.Plugin.Cache;
using SackranyPawn.Plugin.Default;

using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace SackranyPawn.Managers
{
    public static class PawnUpdate
    {
        struct PawnUpdateSystem { }
        struct PawnFixedUpdateSystem { }
        struct PawnLateUpdateSystem { }

        static bool _isUpdating;
        static readonly List<(Pawn pawn, bool add)> _pendingChanges = new();

        static readonly List<Pawn> _localPawns = new();
        static readonly Dictionary<int, int> _localIndex = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _isUpdating = false;
            _pendingChanges.Clear();
            _localPawns.Clear();
            _localIndex.Clear();

            PawnRegister.OnPawnRegistered += OnPawnRegistered;
            PawnRegister.OnPawnUnregistered += OnPawnUnregistered;

            var loop = PlayerLoop.GetCurrentPlayerLoop();

            PlayerLoopUtils.InsertAfter<Update.ScriptRunBehaviourUpdate>(ref loop, new PlayerLoopSystem
            {
                type = typeof(PawnUpdateSystem),
                updateDelegate = Tick
            });
            PlayerLoopUtils.InsertAfter<FixedUpdate.ScriptRunBehaviourFixedUpdate>(ref loop, new PlayerLoopSystem
            {
                type = typeof(PawnFixedUpdateSystem),
                updateDelegate = FixedTick
            });
            PlayerLoopUtils.InsertAfter<PreLateUpdate.ScriptRunBehaviourLateUpdate>(ref loop, new PlayerLoopSystem
            {
                type = typeof(PawnLateUpdateSystem),
                updateDelegate = LateTick
            });

            PlayerLoop.SetPlayerLoop(loop);

            Application.quitting -= CleanUp;
            Application.quitting += CleanUp;
        }
        static void CleanUp()
        {
            PawnRegister.OnPawnRegistered -= OnPawnRegistered;
            PawnRegister.OnPawnUnregistered -= OnPawnUnregistered;

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopUtils.Remove<PawnUpdateSystem>(ref loop);
            PlayerLoopUtils.Remove<PawnFixedUpdateSystem>(ref loop);
            PlayerLoopUtils.Remove<PawnLateUpdateSystem>(ref loop);
            PlayerLoop.SetPlayerLoop(loop);
        }

        static void OnPawnRegistered(Pawn pawn)
        {
            if (_isUpdating) _pendingChanges.Add((pawn, true));
            else AddInternal(pawn);
        }
        static void OnPawnUnregistered(Pawn pawn)
        {
            if (_isUpdating) _pendingChanges.Add((pawn, false));
            else RemoveInternal(pawn);
        }

        static void AddInternal(Pawn pawn)
        {
            if (_localIndex.ContainsKey(pawn.Hash)) return;
            _localIndex[pawn.Hash] = _localPawns.Count;
            _localPawns.Add(pawn);
        }
        static void RemoveInternal(Pawn pawn)
        {
            if (!_localIndex.TryGetValue(pawn.Hash, out int idx)) return;

            int last = _localPawns.Count - 1;
            if (idx != last)
            {
                var swapped = _localPawns[last];
                _localPawns[idx] = swapped;
                _localIndex[swapped.Hash] = idx;
            }
            _localPawns.RemoveAt(last);
            _localIndex.Remove(pawn.Hash);
        }

        static void FlushPending()
        {
            if (_pendingChanges.Count == 0) return;
            for (int i = 0; i < _pendingChanges.Count; i++)
            {
                var (pawn, add) = _pendingChanges[i];
                if (add) AddInternal(pawn);
                else RemoveInternal(pawn);
            }
            _pendingChanges.Clear();
        }

        static void Tick()
        {
            float dt = Time.deltaTime * PawnTimeflow.CurrentTimeFlow;
            var plugins = PluginRegistry.Get<PawnPlugins.IPawnUpdating>.Value;
            var hasAnyPlugins = PluginRegistry.Get<PawnPlugins.IPawnUpdating>.HasAny;
            
            _isUpdating = true;
            for (int i = 0; i < _localPawns.Count; i++)
            {
                if (hasAnyPlugins)
                    for (int p = 0; p < plugins.Length; p++)
                        plugins[p].Execute(_localPawns[i], dt);
                
                _localPawns[i].OnUpdate(dt);
            }
            _isUpdating = false;
            FlushPending();
        }
        static void FixedTick()
        {
            float dt = Time.fixedDeltaTime * PawnTimeflow.CurrentTimeFlow;
            var plugins = PluginRegistry.Get<PawnPlugins.IPawnFixedUpdating>.Value;
            var hasAnyPlugins = PluginRegistry.Get<PawnPlugins.IPawnFixedUpdating>.HasAny;
            
            _isUpdating = true;
            for (int i = 0; i < _localPawns.Count; i++)
            {
                if (hasAnyPlugins)
                    for (int p = 0; p < plugins.Length; p++)
                        plugins[p].Execute(_localPawns[i], dt);
                
                _localPawns[i].OnFixedUpdate(dt);
            }
            _isUpdating = false;
            FlushPending();
        }
        static void LateTick()
        {
            float dt = Time.deltaTime * PawnTimeflow.CurrentTimeFlow;
            var plugins = PluginRegistry.Get<PawnPlugins.IPawnLateUpdating>.Value;
            var hasAnyPlugins = PluginRegistry.Get<PawnPlugins.IPawnLateUpdating>.HasAny;
            
            _isUpdating = true;
            for (int i = 0; i < _localPawns.Count; i++)
            {
                if (hasAnyPlugins)
                    for (int p = 0; p < plugins.Length; p++)
                        plugins[p].Execute(_localPawns[i], dt);
                
                _localPawns[i].OnLateUpdate(dt);
            }
            _isUpdating = false;
            FlushPending();
        }
    }
}