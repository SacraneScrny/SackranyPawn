using System;
using System.Collections.Generic;
using System.Threading;

using Cysharp.Threading.Tasks;

using SackranyPawn.Components;

using UnityEngine;

namespace SackranyPawn.Managers
{
    public static class PawnCmd
    {
        static readonly List<PawnCommand> _commandHandlers = new();
        static CancellationTokenSource _destroyToken;
        static bool _isRunning;
        
        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            _isRunning = false;
            _commandHandlers.Clear();
            _destroyToken = new();

            Application.quitting -= CleanUp;
            Application.quitting += CleanUp;
        }
        static void CleanUp()
        {
            _destroyToken?.Cancel();
            _destroyToken?.Dispose();
        }

        public class PawnCommand
        {
            public Func<Pawn, bool> cond;
            public Action<Pawn> action;
            public readonly List<Action> callbacks = new();
            public bool completed;
            public float Timeout;

            float _time;
            public bool IsTimeOut(float deltaTime)
            {
                _time += deltaTime;
                return _time >= Timeout;
            }
        }
        public class CommandHandle
        {
            readonly PawnCommand _cmd;
            public CommandHandle(PawnCommand cmd) => _cmd = cmd;

            public CommandHandle OnComplete(Action callback)
            {
                if (_cmd.completed) callback?.Invoke();
                else _cmd.callbacks.Add(callback);
                return this;
            }
            public bool Cancel() => _commandHandlers.Remove(_cmd);
        }

        static async UniTaskVoid ProcessLoop()
        {
            _isRunning = true;

            try
            {
                while (_commandHandlers.Count > 0)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, _destroyToken.Token);

                    for (int i = _commandHandlers.Count - 1; i >= 0; i--)
                    {
                        var cmd = _commandHandlers[i];
                        if (cmd.IsTimeOut(Time.deltaTime))
                        {
                            cmd.callbacks.Clear();
                            cmd.completed = true;
                            _commandHandlers.RemoveAt(i);
                            continue;
                        }

                        if (!PawnRegister.TryGetPawn(cmd.cond, out var unit))
                            continue;

                        if (!unit.IsActive)
                            continue;

                        cmd.action(unit);

                        foreach (var callback in cmd.callbacks)
                            callback?.Invoke();

                        cmd.callbacks.Clear();
                        cmd.completed = true;

                        _commandHandlers.RemoveAt(i);
                    }
                }
            }
            catch (OperationCanceledException) { }

            _isRunning = false;
        }

        public static CommandHandle Execute(Func<Pawn, bool> cond, Action<Pawn> action, float timeoutSeconds = 15)
        {
            var cmd = new PawnCommand
            {
                cond = cond,
                action = action
            };

            _commandHandlers.Add(cmd);

            if (!_isRunning)
                ProcessLoop().Forget();

            return new CommandHandle(cmd);
        }
    }
}