using System;
using SackranyPawn.Entities.Modules;

namespace SackranyPawn.Cache
{
    internal static class LimbRegistry
    {
        public static int Count => TypeRegistry<Limb>.Count;
        public static int GetId<T>() where T : Limb => TypeRegistry<Limb>.Id<T>.Value;
        public static int GetId(Type type) => TypeRegistry<Limb>.GetOrRegister(type);
        public static Type GetTypeById(int id) => TypeRegistry<Limb>.GetTypeById(id);
        internal static int LookupId(Type type) => TypeRegistry<Limb>.GetOrRegister(type);
    }
}