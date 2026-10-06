using System;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Cache;
using SackranyPawn.Traits.PawnTags;

namespace SackranyPawn.Entities
{
    public readonly struct TeamInfo : IEquatable<TeamInfo>
    {
        public readonly bool None;
        public readonly int TeamId;

        public TeamInfo(string[] keywords, bool hasTeam = true)
        {
            None = !hasTeam;
            if (None) { TeamId = -1; return; }
            TeamId = Cache.TeamId.Get(keywords);
        }
        public TeamInfo(int teamId, bool none)
        {
            TeamId = teamId;
            None = none;
        }

        public bool Equals(TeamInfo other) => TeamId == other.TeamId;
        public override bool Equals(object obj) => obj is TeamInfo other && Equals(other);
        public static bool operator ==(TeamInfo left, TeamInfo right) => left.Equals(right);
        public static bool operator !=(TeamInfo left, TeamInfo right) => !(left == right);
        public override int GetHashCode() => TeamId;

        public static TeamInfo Default => new TeamInfo(teamId: -1, none: true);
    }
}