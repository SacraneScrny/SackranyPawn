using System;

using UnityEngine.Scripting;

namespace SackranyPawn.Traits.Stats
{
    [Preserve][Serializable] public class AttackDamage     : AStat<AttackDamage>     { }
    [Preserve][Serializable] public class AttackSpeed      : AStat<AttackSpeed>      { }
    [Preserve][Serializable] public class CritChance       : AStat<CritChance>       { }
    [Preserve][Serializable] public class CritMultiplier   : AStat<CritMultiplier>   { }
    [Preserve][Serializable] public class ArmorPenetration : AStat<ArmorPenetration> { }

    [Preserve][Serializable] public class MaxHealth : AStat<MaxHealth> { }
    [Preserve][Serializable] public class Armor     : AStat<Armor>     { }
    [Preserve][Serializable] public class Evasion   : AStat<Evasion>   { }

    [Preserve][Serializable] public class MoveSpeed        : AStat<MoveSpeed>        { }
    [Preserve][Serializable] public class SprintMultiplier : AStat<SprintMultiplier> { }
    [Preserve][Serializable] public class JumpForce        : AStat<JumpForce>        { }
    [Preserve][Serializable] public class Gravity          : AStat<Gravity>          { }

    [Preserve][Serializable] public class CooldownRate   : AStat<CooldownRate>   { }
    [Preserve][Serializable] public class PickupRadius   : AStat<PickupRadius>   { }
    [Preserve][Serializable] public class ExpMultiplier  : AStat<ExpMultiplier>  { }
}