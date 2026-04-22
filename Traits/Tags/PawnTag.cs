using System;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Cache;
using SackranyPawn.Entities;

using UnityEngine;

namespace SackranyPawn.Traits.Tags
{
    [System.Serializable]
    public class PawnTag : APawnData
    {
        [SerializeField][SerializeReference][SubclassSelector]
        ITag[] _defaultTags = Array.Empty<ITag>();

        readonly HashSet<int> _tags = new();

        private protected override void OnInitialize()
        {
            foreach (var tag in _defaultTags)
                _tags.Add(tag.Id);
        }

        public bool HasTag<T>()  where T : ITag => _tags.Contains(TypeRegistry<ITag>.Id<T>.Value);
        public bool HasTag(ITag tag) => _tags.Contains(tag.Id);
        public bool HasTag(int id) => _tags.Contains(id);

        public bool HasAll<TA, TB>() where TA : ITag where TB : ITag
            => HasTag<TA>() && HasTag<TB>();
        public bool HasAll(params ITag[] tags)
            => tags.All(t => HasTag(t.Id));

        public bool HasAny<TA, TB>() where TA : ITag where TB : ITag
            => HasTag<TA>() || HasTag<TB>();
        public bool HasAny(params ITag[] tags)
            => tags.Any(t => HasTag(t.Id));


        public bool Add<T>() where T : ITag => Add(TypeRegistry<ITag>.Id<T>.Value);
        public bool Add(ITag tag) => Add(tag.Id);
        bool Add(int id)
        {
            if (!_tags.Add(id)) return false;
            OnTagAdded?.Invoke(id);
            return true;
        }

        public bool Remove<T>() where T : ITag => Remove(TypeRegistry<ITag>.Id<T>.Value);
        public bool Remove(ITag tag) => Remove(tag.Id);
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