using System;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Cache;
using SackranyPawn.Entities;

using UnityEngine;

namespace SackranyPawn.Traits.PawnTags
{
    [System.Serializable]
    public class PawnTag : APawnData
    {
        [SerializeField][SerializeReference][SubclassSelector]
        IPawnTag[] _defaultTags = Array.Empty<IPawnTag>();

        readonly HashSet<int> _tags = new();

        private protected override void OnInitialize()
        {
            foreach (var tag in _defaultTags)
                _tags.Add(tag.Id);
        }

        public bool HasTag<T>()  where T : IPawnTag => _tags.Contains(TypeRegistry<IPawnTag>.Id<T>.Value);
        public bool HasTag(IPawnTag pawnTag) => _tags.Contains(pawnTag.Id);
        public bool HasTag(int id) => _tags.Contains(id);

        public bool HasAll<TA, TB>() where TA : IPawnTag where TB : IPawnTag
            => HasTag<TA>() && HasTag<TB>();
        public bool HasAll(params IPawnTag[] tags)
            => tags.All(t => HasTag(t.Id));

        public bool HasAny<TA, TB>() where TA : IPawnTag where TB : IPawnTag
            => HasTag<TA>() || HasTag<TB>();
        public bool HasAny(params IPawnTag[] tags)
            => tags.Any(t => HasTag(t.Id));


        public bool Add<T>() where T : IPawnTag => Add(TypeRegistry<IPawnTag>.Id<T>.Value);
        public bool Add(IPawnTag pawnTag) => Add(pawnTag.Id);
        bool Add(int id)
        {
            if (!_tags.Add(id)) return false;
            OnTagAdded?.Invoke(id);
            return true;
        }

        public bool Remove<T>() where T : IPawnTag => Remove(TypeRegistry<IPawnTag>.Id<T>.Value);
        public bool Remove(IPawnTag pawnTag) => Remove(pawnTag.Id);
        bool Remove(int id)
        {
            if (!_tags.Remove(id)) return false;
            OnTagRemoved?.Invoke(id);
            return true;
        }

        public IEnumerable<int> GetIds() => _tags;
        public override void Reset()
        {
            foreach (var tag in _tags)
                OnTagRemoved?.Invoke(tag);
            _tags.Clear();
            foreach (var tag in _defaultTags)
                _tags.Add(tag.Id);
        }
        
        public event Action<int> OnTagAdded;
        public event Action<int> OnTagRemoved;
    }
}