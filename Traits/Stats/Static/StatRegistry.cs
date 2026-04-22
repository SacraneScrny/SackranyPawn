using System;

using SackranyPawn.Cache;

namespace SackranyPawn.Traits.Stats.Static
{
    public static class StatRegistry
    {
        public static int Count => TypeRegistry<IStat>.Count;
        public static int GetId<T>() where T : IStat => TypeRegistry<IStat>.Id<T>.Value;
        public static int GetId(Type type) => TypeRegistry<IStat>.GetOrRegister(type);
        public static Type GetTypeById(int id) => TypeRegistry<IStat>.GetTypeById(id);
    }
}