using System;
using System.Collections.Generic;

using SackranyPawn.Entities.Modules;
using SackranyPawn.Entities.Modules.ModuleComposition;
using SackranyPawn.Traits.Fluxes.Cache;
using SackranyPawn.Traits.Fluxes.Entities;

using UnityEngine;

namespace SackranyPawn.Traits.Fluxes
{
    [Serializable]
    public class FluxHandler : AsyncLimb, IUpdateLimb, IFixedUpdateLimb
    {
        [SerializeField] [SerializeReference] [SubclassSelector] List<Flux> Fluxes;
        
        readonly Dictionary<int, int> _fluxIndex = new();
        readonly Dictionary<int, HashSet<FluxHandle>> _fluxesByIds = new();
        
        public float CachedDeltaTime { get; private set; }
        public float CachedFixedDeltaTime { get; private set; }
        
        protected override void OnStart()
        {
            foreach (var flux in Fluxes)
            {
                CacheInternal(flux);
                flux.Initialize(this, 1);
                flux.Start();
            }    
        }
        
        protected override void OnReset()
        {
            foreach (var flux in Fluxes)
                flux.Dispose();
            Fluxes.Clear();
            _fluxesByIds.Clear();
            _fluxIndex.Clear();
            FluxAdded = null;
            FluxRemoved = null;
        }

        #region FLUXES
        public FluxScope[] ApplyFluxes(Flux[] flux, params int[] amounts)
        {
            List<FluxScope> disposables = new List<FluxScope>();
            for (int i = 0; i < flux.Length; i++)
            {
                int amnt = i >= amounts.Length ? 1 : amounts[i];
                if (amnt == 0) continue;
                
                disposables.Add(ApplyFlux(flux[i], amnt));
            }
            return disposables.ToArray();
        }
        public FluxScope ApplyFlux(Flux flux, int amount = 1)
        {
            if (amount <= 0) return default;
            var instance = (Flux)flux.Clone();
            
            CacheInternal(instance);
            instance.Initialize(this, amount);
            instance.Start();
            Fluxes.Add(instance);
            FluxAdded?.Invoke(instance);
            return new FluxScope(instance, RemoveInternal);
        }
        public FluxScope ApplyFlux<T>(int amount = 1) where T : Flux, new()
        {
            return ApplyFlux(FluxRegistry.GetTemplate<T>(), amount);
        }

        public bool RemoveFlux(FluxHandle flux) => flux is Flux f && RemoveInternal(f);
        public void RemoveAllFluxes()
        {
            for (int i = 0; i < Fluxes.Count; i++)
            {
                Fluxes[i]?.Dispose();
            }
            Fluxes.Clear();
            _fluxesByIds.Clear();
            _fluxIndex.Clear();
        }
        
        public IReadOnlyCollection<FluxHandle> GetFluxesById(int id) 
            => _fluxesByIds.GetValueOrDefault(id) ?? (IReadOnlyCollection<FluxHandle>)Array.Empty<FluxHandle>();
        public bool TryGetFluxesById(int id, out IReadOnlyCollection<FluxHandle> result)
        {
            if (_fluxesByIds.TryGetValue(id, out var set))
            {
                result = set;
                return true;
            }
            result = null;
            return false;
        }

        public IReadOnlyCollection<FluxHandle> GetFluxes<T>() where T : Flux 
            => _fluxesByIds.GetValueOrDefault(FluxRegistry.GetId<T>()) ?? (IReadOnlyCollection<FluxHandle>)Array.Empty<FluxHandle>();
        public bool TryGetFluxes<T>(out IReadOnlyCollection<FluxHandle> result) where T : Flux
        {
            if (_fluxesByIds.TryGetValue(FluxRegistry.GetId<T>(), out var set))
            {
                result = set;
                return true;
            }
            result = null;
            return false;
        }
        #endregion

        #region INTERNAL
        void CacheInternal(Flux flux)
        {
            if (!_fluxesByIds.TryGetValue(flux.Id, out var fluxes))
            {
                fluxes = new();
                _fluxesByIds.Add(flux.Id, fluxes);
            }
            fluxes.Add(flux);
            _fluxIndex[flux.GetHashCode()] = Fluxes.Count;
        }
        void RemoveFromCacheInternal(Flux flux)
        {
            if (!_fluxesByIds.TryGetValue(flux.Id, out var fluxes))
            {
                return;
            }
            fluxes.Remove(flux);
            if (fluxes.Count == 0)
                _fluxesByIds.Remove(flux.Id);
        }
        
        bool RemoveInternal(Flux flux)
        {
            if (flux == null) return false;
            RemoveFromCacheInternal(flux);

            int key = flux.GetHashCode();
            if (!_fluxIndex.TryGetValue(key, out int idx)) return false;

            int last = Fluxes.Count - 1;
            if (idx != last)
            {
                var swapped = Fluxes[last];
                Fluxes[idx] = swapped;
                _fluxIndex[swapped.GetHashCode()] = idx;
            }
            Fluxes.RemoveAt(last);
            _fluxIndex.Remove(key);

            FluxRemoved?.Invoke(flux);
            flux.Dispose();
            return true;
        }
        #endregion
        
        public void OnUpdate(float deltaTime)
        {
            CachedDeltaTime = deltaTime;
        }
        public void OnFixedUpdate(float deltaTime)
        {
            CachedFixedDeltaTime = deltaTime;
        }
        
        public event Action<FluxHandle> FluxAdded; 
        public event Action<FluxHandle> FluxRemoved;
        
        protected override void OnDispose()
        {
            RemoveAllFluxes();    
        }
    }
}