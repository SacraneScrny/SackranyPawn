using System;

using SackranyPawn.Entities.Modules;
using SackranyPawn.Traits.Stats.Static;

namespace SackranyPawn.Traits.Stats
{
    public interface IStat
    {
        int Id { get; }
    }
    [Serializable]
    public abstract class AStat : IStat
    {
        public abstract int Id { get; }
        [HashKey] public float baseValue;
    }
    [Serializable]
    public abstract class AStat<TSelf> : AStat
        where TSelf : AStat<TSelf>
    {
        public override int Id => StatRegistry.GetId<TSelf>();

        public sealed override bool Equals(object obj) => obj is IStat other && other.Id == Id;
        public sealed override int GetHashCode() => Id.GetHashCode();
    }
}