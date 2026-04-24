using System;
using System.Collections.Generic;

using SackranyPawn.Components;
using SackranyPawn.Entities.Modules;
using SackranyPawn.Managers;
using SackranyPawn.Traits.PawnTags;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SackranyPawn.Entities
{
    /// <summary>
    /// Fluent builder for creating and configuring Pawn instances.
    ///
    /// Three creation strategies:
    ///   PawnBuilder.FromPrefab(prefab) — Instantiate from a prefab
    ///   PawnBuilder.FromPool(prefab)   — Pop from PawnPool, reconfigure
    ///   PawnBuilder.New("name")        — Create an empty GameObject + Pawn
    ///
    /// Usage:
    ///   var pawn = PawnBuilder.FromPrefab(enemyPrefab)
    ///       .At(spawnPoint)
    ///       .WithTag&lt;Tags.Enemy&gt;()
    ///       .WithLimb(new HealthLimb { Max = 100 })
    ///       .Run()
    ///       .Build();
    /// </summary>
    public sealed class PawnBuilder
    {
        enum SourceKind { Prefab, Pool, New }

        SourceKind _source;
        Pawn _prefab;
        string _newName = "Pawn";

        bool _hasTransform;
        Vector3 _position;
        Quaternion _rotation = Quaternion.identity;
        Transform _parent;
        bool _worldPositionStays = true;

        readonly List<Limb> _limbs = new();
        readonly List<Action<PawnTag>> _tagActions = new();

        bool _workByDefault;
        bool _autoStart;

        PawnBuilder() { }

        // ── Factory ──────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiate a new Pawn from <paramref name="prefab"/> via Object.Instantiate.
        /// </summary>
        public static PawnBuilder FromPrefab(Pawn prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return new PawnBuilder { _source = SourceKind.Prefab, _prefab = prefab };
        }

        /// <inheritdoc cref="FromPrefab(Pawn)"/>
        public static PawnBuilder FromPrefab(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var pawn = prefab.GetComponent<Pawn>();
            if (pawn == null) throw new ArgumentException("GameObject has no Pawn component.", nameof(prefab));
            return FromPrefab(pawn);
        }

        /// <summary>
        /// Pop a Pawn from <see cref="PawnPool"/> using <paramref name="prefab"/> as template.
        /// The builder's lifecycle settings (WorkByDefault / AutoStart) fully override
        /// whatever the template had.
        /// </summary>
        public static PawnBuilder FromPool(Pawn prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return new PawnBuilder { _source = SourceKind.Pool, _prefab = prefab };
        }

        /// <inheritdoc cref="FromPool(Pawn)"/>
        public static PawnBuilder FromPool(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var pawn = prefab.GetComponent<Pawn>();
            if (pawn == null) throw new ArgumentException("GameObject has no Pawn component.", nameof(prefab));
            return FromPool(pawn);
        }

        /// <summary>
        /// Create a brand-new empty GameObject with a fresh Pawn component attached.
        /// </summary>
        public static PawnBuilder New(string name = "Pawn")
        {
            return new PawnBuilder { _source = SourceKind.New, _newName = name };
        }

        // ── Transform ────────────────────────────────────────────────────────

        public PawnBuilder At(Vector3 position)
        {
            _position = position;
            _hasTransform = true;
            return this;
        }

        public PawnBuilder At(Vector3 position, Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
            _hasTransform = true;
            return this;
        }

        public PawnBuilder At(Transform point)
        {
            _position = point.position;
            _rotation = point.rotation;
            _hasTransform = true;
            return this;
        }

        /// <param name="worldPositionStays">
        /// Passed to Transform.SetParent. When false the local position is preserved.
        /// </param>
        public PawnBuilder UnderParent(Transform parent, bool worldPositionStays = true)
        {
            _parent = parent;
            _worldPositionStays = worldPositionStays;
            return this;
        }

        // ── Limbs ────────────────────────────────────────────────────────────

        /// <summary>
        /// Add a default-constructed Limb of type <typeparamref name="T"/>.
        /// </summary>
        public PawnBuilder WithLimb<T>() where T : Limb, new()
        {
            _limbs.Add(new T());
            return this;
        }

        /// <summary>
        /// Add a pre-configured Limb instance.
        /// </summary>
        public PawnBuilder WithLimb(Limb limb)
        {
            if (limb != null) _limbs.Add(limb);
            return this;
        }

        /// <summary>
        /// Add multiple Limb instances at once.
        /// </summary>
        public PawnBuilder WithLimbs(params Limb[] limbs)
        {
            if (limbs == null) return this;
            foreach (var l in limbs)
                if (l != null) _limbs.Add(l);
            return this;
        }

        // ── Tags ─────────────────────────────────────────────────────────────

        public PawnBuilder WithTag<T>() where T : IPawnTag
        {
            _tagActions.Add(tag => tag.Add<T>());
            return this;
        }

        public PawnBuilder WithoutTag<T>() where T : IPawnTag
        {
            _tagActions.Add(tag => tag.Remove<T>());
            return this;
        }

        public PawnBuilder WithTags(params IPawnTag[] tags)
        {
            if (tags == null) return this;
            foreach (var t in tags)
            {
                var captured = t;
                _tagActions.Add(tag => tag.Add(captured));
            }
            return this;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        /// <summary>
        /// Set whether the Pawn auto-starts in future resets (pool / scene reload).
        /// </summary>
        public PawnBuilder WorkByDefault(bool value = true)
        {
            _workByDefault = value;
            return this;
        }

        /// <summary>
        /// Call StartWork() immediately after Build().
        /// Does not imply WorkByDefault — configure that separately if needed.
        /// </summary>
        public PawnBuilder AutoStart(bool value = true)
        {
            _autoStart = value;
            return this;
        }

        /// <summary>
        /// Shorthand for WorkByDefault(true).AutoStart(true).
        /// Mirrors Pawn.Run() behaviour.
        /// </summary>
        public PawnBuilder Run()
        {
            _workByDefault = true;
            _autoStart = true;
            return this;
        }

        // ── Build ─────────────────────────────────────────────────────────────

        public Pawn Build()
        {
            var pawn = Create();
            if (pawn == null) return null;

            pawn.Initialize();
            ApplyTransform(pawn);
            ApplyTags(pawn);
            ApplyLimbs(pawn);
            ApplyLifecycle(pawn);

            return pawn;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        Pawn Create()
        {
            switch (_source)
            {
                case SourceKind.Prefab: return CreateFromPrefab();
                case SourceKind.Pool: return CreateFromPool();
                case SourceKind.New: return CreateNew();
                default: return null;
            }
        }

        Pawn CreateFromPrefab()
        {
            return Object.Instantiate(_prefab);
        }

        Pawn CreateFromPool()
        {
            var pawn = PawnPool.Pop(_prefab);
            pawn.StopWork();
            return pawn;
        }

        Pawn CreateNew()
        {
            var go = new GameObject(_newName);
            return go.AddComponent<Pawn>();
        }

        void ApplyTransform(Pawn pawn)
        {
            if (_parent != null)
                pawn.transform.SetParent(_parent, _worldPositionStays);

            if (_hasTransform)
                pawn.transform.SetPositionAndRotation(_position, _rotation);
        }

        void ApplyTags(Pawn pawn)
        {
            foreach (var action in _tagActions)
                action(pawn.Tag);
        }

        void ApplyLimbs(Pawn pawn)
        {
            if (_limbs.Count == 0) return;

            var body = pawn.GetBody();
            body.Start();
            body.Add(_limbs.ToArray(), false);
        }

        void ApplyLifecycle(Pawn pawn)
        {
            pawn.SetWorkByDefault(_workByDefault);
            if (_autoStart) pawn.StartWork();
        }
    }
}
