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
        
        readonly Dictionary<int, List<FluxHandle>> _fluxesByIds = new();
        
        public float CachedDeltaTime { get; private set; }
        public float CachedFixedDeltaTime { get; private set; }
        
        protected override void OnReset()
        {
            foreach (var flux in Fluxes)
                flux.Dispose();
            Fluxes.Clear();
            _fluxesByIds.Clear();
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
        }
        
        public IReadOnlyList<FluxHandle> GetFluxesById(int id) => _fluxesByIds.GetValueOrDefault(id);
        public bool TryGetFluxesById(int id, out IReadOnlyList<FluxHandle> result)
        {
            if (_fluxesByIds.TryGetValue(id, out var list))
            {
                result = list;
                return true;
            }
            result = null;
            return false;
        }

        public IReadOnlyList<FluxHandle> GetFluxes<T>() where T : Flux => _fluxesByIds.GetValueOrDefault(FluxRegistry.GetId<T>());
        public bool TryGetFluxes<T>(out IReadOnlyList<FluxHandle> result) where T : Flux
        {
            if (_fluxesByIds.TryGetValue(FluxRegistry.GetId<T>(), out var list))
            {
                result = list;
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
                fluxes = new ();
                _fluxesByIds.Add(flux.Id, fluxes);
            }
            fluxes.Add(flux);
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
            var ret = Fluxes.Remove(flux);
            FluxRemoved?.Invoke(flux);
            flux.Dispose();
            return ret;
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