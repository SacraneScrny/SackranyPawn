using UnityEngine.Scripting;

namespace SackranyPawn.Traits.PawnEvents
{
    public static partial class Events
    {
        [Preserve] public class OnDamage : AEvent<OnDamage> { }
        [Preserve] public class OnDamagePostProcess : AEvent<OnDamagePostProcess> { }
        [Preserve] public class OnHeal : AEvent<OnHeal> { }
        [Preserve] public class OnDied : AEvent<OnDied> { }
    }
}