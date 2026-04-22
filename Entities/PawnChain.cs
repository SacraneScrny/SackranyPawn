using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using SackranyPawn.Components;
using SackranyPawn.Entities.Modules;
using SackranyPawn.Managers;

namespace SackranyPawn.Entities
{
    public readonly struct PawnChain
    {
        readonly Pawn _pawn;
        readonly bool _valid;

        public PawnChain(Pawn pawn)
        {
            _pawn  = pawn;
            _valid = pawn != null && pawn.IsActive;
        }

        public static PawnChain From(Pawn pawn) => new(pawn);
        public static PawnChain Find(Func<Pawn, bool> p) => new(PawnRegister.GetPawn(u => u.IsActive && p(u)));
        public PawnChain FlatMap(Func<Pawn, PawnChain> selector)
            => _valid ? selector(_pawn) : default;

        public PawnChain Where(Func<Pawn, bool> predicate)
            => _valid && predicate(_pawn) ? this : default;
        public PawnChain Where<TLimb>(Func<TLimb, bool> predicate) where TLimb : Limb
            => _valid && _pawn.TryGet(out TLimb m) && predicate(m) ? this : default;
        public PawnChain Has<TLimb>() where TLimb : Limb
            => _valid && _pawn.Has<TLimb>() ? this : default;

        public PawnChain Do(Action<Pawn> action)
        {
            if (_valid) action(_pawn);
            return this;
        }
        public PawnChain Do<TLimb>(Action<TLimb> action) where TLimb : Limb
        {
            if (_valid && _pawn.TryGet(out TLimb m)) action(m);
            return this;
        }
        public PawnChain Do<TA, TB>(Action<TA, TB> action)
            where TA : Limb where TB : Limb
        {
            if (_valid && _pawn.TryGet(out TA a) && _pawn.TryGet(out TB b)) action(a, b);
            return this;
        }
        
        public LimbChain<TLimb> Limb<TLimb>() where TLimb : Limb
        {
            if (_valid && _pawn.TryGet(out TLimb m)) return new LimbChain<TLimb>(m, _pawn);
            return default;
        }
        
        public PawnChain Select(Func<Pawn, Pawn> selector)
            => _valid ? new PawnChain(selector(_pawn)) : default;
        public PawnChain Branch(Func<Pawn, bool> predicate, Action<PawnChain> onTrue, Action<PawnChain> onFalse = null)
        {
            if (!_valid) return this;
            if (predicate(_pawn)) onTrue(this);
            else onFalse?.Invoke(this);
            return this;
        }
        public PawnChain Tap(Action<Pawn> action) => Do(action);
        
        public TResult Get<TLimb, TResult>(Func<TLimb, TResult> selector, TResult fallback = default)
            where TLimb : Limb
            => _valid && _pawn.TryGet(out TLimb m) ? selector(m) : fallback;
        public TResult Get<TResult>(Func<Pawn, TResult> selector, TResult fallback = default)
            => _valid ? selector(_pawn) : fallback;
        public bool TryGet(out Pawn pawn)
        {
            pawn = _valid ? _pawn : null;
            return _valid;
        }
        
        public async UniTask<PawnChain> DoAsync(Func<Pawn, UniTask> action)
        {
            if (_valid) await action(_pawn);
            return this;
        }
        public async UniTask<PawnChain> DoAsync<TLimb>(Func<TLimb, UniTask> action) where TLimb : Limb
        {
            if (_valid && _pawn.TryGet(out TLimb m)) await action(m);
            return this;
        }
        public async UniTask<PawnChain> WaitActive(CancellationToken token = default)
        {
            if (_pawn == null) return default;
            var pawn = _pawn;
            await UniTask.WaitWhile(() => !pawn.IsActive, cancellationToken: token);
            return new PawnChain(_pawn);
        }

        public bool IsValid => _valid;
        public Pawn Value => _valid ? _pawn : null;

        public static implicit operator bool(PawnChain chain) => chain._valid;
        public static implicit operator Pawn(PawnChain chain) => chain.Value;
        public static implicit operator PawnChain(Pawn pawn) => new(pawn);
    }
    
    public readonly struct LimbChain<TLimb> where TLimb : Limb
    {
        readonly TLimb _limb;
        readonly Pawn _pawn;
        readonly bool _valid;

        internal LimbChain(TLimb limb, Pawn pawn)
        {
            _limb = limb;
            _pawn = pawn;
            _valid = limb != null;
        }

        public LimbChain<TLimb> Where(Func<TLimb, bool> predicate)
            => _valid && predicate(_limb) ? this : default;
        public LimbChain<TLimb> Do(Action<TLimb> action)
        {
            if (_valid) action(_limb);
            return this;
        }
        public LimbChain<TLimb> Do(Action<TLimb, Pawn> action)
        {
            if (_valid) action(_limb, _pawn);
            return this;
        }

        public TResult Get<TResult>(Func<TLimb, TResult> selector, TResult fallback = default)
            => _valid ? selector(_limb) : fallback;
        public bool TryGet(out TLimb module) { module = _valid ? _limb : null; return _valid; }

        public LimbChain<TOther> Switch<TOther>() where TOther : Limb
            => _valid ? new PawnChain(_pawn).Limb<TOther>() : default;
        public PawnChain Back() => _valid ? PawnChain.From(_pawn) : default;

        public async UniTask<LimbChain<TLimb>> DoAsync(Func<TLimb, UniTask> action)
        {
            if (_valid) await action(_limb);
            return this;
        }

        public bool IsValid => _valid;

        public static implicit operator bool(LimbChain<TLimb> c) => c._valid;
    }
}