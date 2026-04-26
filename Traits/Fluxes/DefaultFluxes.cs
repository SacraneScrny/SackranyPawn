using System;

using ModifiableVariable.Entities;
using ModifiableVariable.Stages.StageFactory;

using SackranyPawn.Traits.Conditions;
using SackranyPawn.Traits.Fluxes.Entities;
using SackranyPawn.Traits.Stats;

using UnityEngine;

namespace SackranyPawn.Traits.Fluxes
{
    [Serializable]
    public sealed class ConditionBlockFlux : Flux<ConditionBlockFlux>
    {
        [SerializeReference] [SubclassSelector] public ACondition Condition;

        protected override void OnStart() =>
            Handler.Pawn.Block(Condition);

        protected override void OnDisposing() =>
            Handler.Pawn.Unblock(Condition);
    }

    [Serializable]
    public sealed class StatModifierFlux : Flux<StatModifierFlux>
    {
        [SerializeReference] [SubclassSelector] public AStat Stat;
        public float Value;
        public General Stage;

        ModifierDelegateHandler<float> _handle;

        protected override void OnStart()
        {
            var stat = Handler.Pawn.GetStat(Stat);
            if (stat == null) return;
            float v = Value;
            _handle = stat.Add(() => v, Stage);
        }

        protected override void OnDisposing()
        {
            var stat = Handler.Pawn.GetStat(Stat);
            stat?.Remove(_handle);
        }
    }
}