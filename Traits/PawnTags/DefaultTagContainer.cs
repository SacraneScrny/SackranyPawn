using System;

using UnityEngine.Scripting;

namespace SackranyPawn.Traits.PawnTags
{
    public static partial class Tags
    {
        [Serializable] [Preserve] public class Enemy : PawnTag<Enemy> { }
        [Serializable] [Preserve] public class Player : PawnTag<Player> { }
        [Serializable] [Preserve] public class Undead : PawnTag<Undead> { }
        [Serializable] [Preserve] public class Flying : PawnTag<Flying> { }
    }
}