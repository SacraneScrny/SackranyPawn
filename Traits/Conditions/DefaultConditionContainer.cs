using System;

using UnityEngine.Scripting;

namespace SackranyPawn.Traits.Conditions
{
    [Preserve][Serializable] public class CanAct : ACondition<CanAct>    { }
    [Preserve][Serializable] public class CanMove : ACondition<CanMove>   { }
    [Preserve][Serializable] public class CanAttack : ACondition<CanAttack> { }
    [Preserve][Serializable] public class CanCast : ACondition<CanCast>   { }
    [Preserve][Serializable] public class CanBeHit : ACondition<CanBeHit>  { }
    [Preserve][Serializable] public class CanDie : ACondition<CanDie>    { }
}