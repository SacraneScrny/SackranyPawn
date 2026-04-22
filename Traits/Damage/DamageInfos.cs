using JetBrains.Annotations;

using SackranyPawn.Components;

using UnityEngine;

namespace SackranyPawn.Traits.Damage
{
    public interface IDamage { }
    public interface IDamage<out T> : IDamage
        where T : struct
    {
        T Damage { get; }
        Vector3 Direction { get; }
        Vector3 HitPosition { get; }
        Vector3 AttackPosition { get; }
        
        Pawn Attacker { get; }
        Pawn Target { get; }
        GameObject GameObject { get; }

        bool HitSomebody();
        bool HitSelf();
        bool FriendlyFire();
    }
    public readonly struct DamageInfo : IDamage<float>
    {
        public float Damage { get; }
        public Vector3 Direction { get; }
        public Vector3 HitPosition { get; }
        public Vector3 AttackPosition { get; }

        public Pawn Attacker { get; }
        public Pawn Target { get; }
        public GameObject GameObject { get; }
        
        public bool HitSomebody() => Target != null;
        public bool HitSelf() => HitSomebody() && Attacker == Target;
        public bool FriendlyFire() => HitSomebody() && Attacker.Team == Target.Team;

        public DamageInfo(
            float damage,
            Vector3? hitPosition,
            Vector3? attackPosition,
            [CanBeNull] GameObject gameObject,
            [CanBeNull] Pawn attacker,
            [CanBeNull] Pawn target)
        {
            this.GameObject = gameObject ?? target?.gameObject ?? attacker?.gameObject;
            Damage = damage;
            
            Attacker = attacker;
            Target = target;
            
            if (hitPosition.HasValue)
                HitPosition = hitPosition.Value;
            else if (target != null) HitPosition = target.transform.position;
            else if (attacker != null) HitPosition = attacker.transform.position;
            else HitPosition = Vector3.zero;
            
            if (attackPosition.HasValue)
                AttackPosition = attackPosition.Value;
            else if (attacker != null) AttackPosition = attacker.transform.position;
            else if (target != null) AttackPosition = target.transform.position;
            else AttackPosition = Vector3.zero;

            Direction = HitPosition - AttackPosition;
        }
    }

    public enum DamageType
    {
        DamageInfo
    }
}