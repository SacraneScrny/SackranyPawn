using SackranyPawn.Cache;
using SackranyPawn.Plugin.Cache;
using UnityEngine;

namespace SackranyPawn.Managers
{
    public static class PawnBoostrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Boot()
        {
            TeamId.Clear();
            TypeRegistryWarmup.Warmup();
            PluginRegistry.Init();
            PawnHash.Init();
            PawnRegister.Init();
            PawnPool.Init();

            PawnUpdate.Init();
            PawnTimeflow.Init();
            PawnSpatial.Init();

            PawnCmd.Init();
        }
    }
}