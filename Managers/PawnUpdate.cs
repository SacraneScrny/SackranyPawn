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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
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
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopUtils.Remove<PawnUpdateSystem>(ref loop);
            PlayerLoopUtils.Remove<PawnFixedUpdateSystem>(ref loop);
            PlayerLoopUtils.Remove<PawnLateUpdateSystem>(ref loop);
            PlayerLoop.SetPlayerLoop(loop);
        }

        static void Tick()
        {
            float dt = Time.deltaTime * PawnTimeflow.CurrentTimeFlow;
            var pawns = PawnRegister.RegisteredPawns;
            for (int i = 0; i < pawns.Count; i++)
                pawns[i].OnUpdate(dt);
        }

        static void FixedTick()
        {
            float dt = Time.fixedDeltaTime * PawnTimeflow.CurrentTimeFlow;
            var pawns = PawnRegister.RegisteredPawns;
            for (int i = 0; i < pawns.Count; i++)
                pawns[i].OnFixedUpdate(dt);
        }

        static void LateTick()
        {
            float dt = Time.deltaTime * PawnTimeflow.CurrentTimeFlow;
            var pawns = PawnRegister.RegisteredPawns;
            for (int i = 0; i < pawns.Count; i++)
                pawns[i].OnLateUpdate(dt);
        }
    }
}