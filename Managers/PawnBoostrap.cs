using SackranyPawn.Cache;
using SackranyPawn.Plugin.Cache;
using SackranyPawn.Traits.Fluxes.Cache;

using UnityEngine;

namespace SackranyPawn.Managers
{
    public static class PawnBoostrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Boot()
        {
            TypeRegistryWarmup.Warmup();
            PluginRegistry.Init();
            PawnHash.Init();
            PawnRegister.Init();
            PawnPool.Init();

            PawnUpdate.Init();
            PawnTimeflow.Init();
            PawnSpatial.Init();

            FluxRegistry.ResetTemplates();
            PawnCmd.Init();
        }
    }
}