using System;

using SackranyPawn.Cache;

namespace SackranyPawn.Traits.PawnTags
{
    [Serializable]
    public abstract class PawnTag<TSelf> : IPawnTag where TSelf : PawnTag<TSelf>
    {
        public int Id => TypeRegistry<IPawnTag>.Id<TSelf>.Value;
    }

    public interface IPawnTag { int Id { get; } }
}