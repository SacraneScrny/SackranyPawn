using System;

using Sackrany.Utils.CacheRegistry;

namespace SackranyPawn.Traits.Tags
{
    [Serializable]
    public abstract class Tag<TSelf> : ITag where TSelf : Tag<TSelf>
    {
        public int Id => TypeRegistry<ITag>.Id<TSelf>.Value;
    }

    public interface ITag { int Id { get; } }
}