using System;

using SackranyPawn.Cache;
using SackranyPawn.Components;

namespace SackranyPawn.Entities
{
    public readonly struct PawnArchetype : IEquatable<PawnArchetype>
    {
        public readonly uint Hash;

        public PawnArchetype(Pawn pawn)
        {
            Hash = ArchetypeCache.GetHash(pawn);
        }

        public bool Equals(PawnArchetype other) => Hash == other.Hash;
        public override bool Equals(object obj) => obj is PawnArchetype other && Equals(other);
        public override int GetHashCode() => unchecked((int)Hash);
        public static bool operator ==(PawnArchetype l, PawnArchetype r) => l.Equals(r);
        public static bool operator !=(PawnArchetype l, PawnArchetype r) => !(l == r);
    }
}