using SackranyPawn.Entities.Modules;
using SackranyPawn.Traits.PawnEvents;

namespace SackranyPawn.Traits.Damage
{
    public class DamageListener<T> : Limb
        where T : struct
    {
        [Dependency] protected IDamageListener<T>[] _damageListeners;
        public readonly SequentialRewriteVariable<T> DamageOverwrite = new ();
        
        protected sealed override void OnStart()
        {
            Pawn.Event.Subscribe<Events.OnDamage, T>(ProcessDamage);
        }
        public void ProcessDamage(T damage)
        {
            damage = ProcessDamage_Internal(damage);
            OnDamage?.Invoke(damage);
            Pawn.Event.Publish<Events.OnDamagePostProcess, T>(damage);
        }
        T ProcessDamage_Internal(T damage)
        {
            var newDmg = DamageOverwrite.Calculate(damage);
            for (int i = 0; i < _damageListeners.Length; i++)
                _damageListeners[i].ProceedDamage(newDmg);
            return newDmg;
        }
        
        protected sealed override void OnReset()
        {
            OnDamage = null;
            DamageOverwrite.Clear();
        }
        
        public delegate void DamageHandler(T damage);
        public event DamageHandler OnDamage;
    }
    
    public class DefaultDamageListener : DamageListener<DamageInfo> { }
    
    public interface IDamageListener<in T>
        where T : struct
    {
        public void ProceedDamage(T damage);
    }
}