using ModifiableVariable;

using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace SackranyPawn.Managers
{
    public static class PawnTimeflow
    {
        struct PawnTimeFlowUpdateSystem { }
        public static readonly Modifiable<float> TimeFlow = new (1f);
        
        static float _lastCurrentTimeFlow = 0f;
        public static float CurrentTimeFlow => _lastCurrentTimeFlow;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            TimeFlow.Clear();
            _lastCurrentTimeFlow = TimeFlow;            
            
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopUtils.InsertAfter<Update.ScriptRunBehaviourUpdate>(ref loop, new PlayerLoopSystem
            {
                type = typeof(PawnTimeFlowUpdateSystem),
                updateDelegate = Tick
            });

            PlayerLoop.SetPlayerLoop(loop);
            
            Application.quitting -= CleanUp;
            Application.quitting += CleanUp;
        }
        static void CleanUp()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopUtils.Remove<PawnTimeFlowUpdateSystem>(ref loop);
            PlayerLoop.SetPlayerLoop(loop);
        }
        
        static void Tick()
        {
            _lastCurrentTimeFlow = TimeFlow;
        }
    }
}