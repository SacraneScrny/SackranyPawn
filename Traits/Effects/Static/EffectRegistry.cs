using System;

using Sackrany.Utils.CacheRegistry;

namespace SackranyPawn.Traits.Effects.Static
{
    public static class EffectRegistry
    {
        public static int Count                        => TypeRegistry<Effect>.Count;
        public static int GetId<T>() where T : Effect  => TypeRegistry<Effect>.Id<T>.Value;
        public static int GetId(Type type)             => TypeRegistry<Effect>.GetOrRegister(type);
        public static Type GetTypeById(int id)         => TypeRegistry<Effect>.GetTypeById(id);
        internal static int LookupId(Type type)        => TypeRegistry<Effect>.GetOrRegister(type);
    }
}