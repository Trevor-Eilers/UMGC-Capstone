// Author: Trevor Eilers

using System.Collections.Generic;
using Core;
using Simulation;
using Unity.Netcode;
using UnityEngine;

namespace Building
{
    public class BuildingSystem : MonoBehaviour
    {
        public BuildingSpawner spawner;

        private const float PopPerPlot = 12f;
        private const int MaxBuildingsPerTick = 3;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private readonly List<Renderer> _renderers = new();
        private MaterialPropertyBlock _propBlock;
        private float _lastHealth = float.NaN;
        private Color _currentTint = Color.white;

        private District _district;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
        }

        public District District
        {
            get => _district;
            set
            {
                if (_district == value) return;
                Unsubscribe();
                _district = value;
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (_district == null || spawner == null) return;
            _district.placements.OnListChanged += OnPlacementsChanged;
            _district.state.OnValueChanged += OnStateChanged;

            for (int i = 0; i < _district.placements.Count; i++)
            {
                var go = spawner.ApplyPlacement(_district.placements[i]);
                if (go != null) CollectRenderers(go, tintNow: false);
            }

            ApplyTinting(_district.state.Value);
        }

        private void Unsubscribe()
        {
            if (_district == null) return;
            _district.placements.OnListChanged -= OnPlacementsChanged;
            _district.state.OnValueChanged -= OnStateChanged;
        }

        private void OnStateChanged(DistrictState prev, DistrictState next) => ApplyTinting(next);

        private void OnDestroy() => Unsubscribe();

        private void OnPlacementsChanged(NetworkListEvent<BuildingPlacement> evt)
        {
            switch (evt.Type)
            {
                case NetworkListEvent<BuildingPlacement>.EventType.Add:
                    var go = spawner.ApplyPlacement(evt.Value);
                    if (go != null) CollectRenderers(go, tintNow: true);
                    break;
                case NetworkListEvent<BuildingPlacement>.EventType.RemoveAt:
                case NetworkListEvent<BuildingPlacement>.EventType.Remove:
                    spawner.RevertPlacement(evt.Value);
                    break;
                case NetworkListEvent<BuildingPlacement>.EventType.Clear:
                    break;
            }
        }

        public void UpdateDistrict(DistrictState state)
        {
            if (spawner == null || _district == null) return;

            int totalPlots = spawner.TotalPlotCount();
            if (totalPlots == 0) return;

            int targetOccupied = Mathf.Clamp(
                Mathf.RoundToInt(state.population / PopPerPlot), 0, totalPlots);
            int currentOccupied = _district.placements.Count;

            if (currentOccupied < targetOccupied)
            {
                var max = Random.Range(1, MaxBuildingsPerTick);
                int toSpawn = Mathf.Min(targetOccupied - currentOccupied, max);
                for (int s = 0; s < toSpawn; s++)
                {
                    var type = NextType(s);
                    if (!spawner.TrySpawnRandom(_district, type, SelectTier(type, state))) break;
                }
            }
            else if (currentOccupied > targetOccupied)
            {
                var max = Random.Range(1, MaxBuildingsPerTick);
                int toRemove = Mathf.Min(currentOccupied - targetOccupied, MaxBuildingsPerTick);
                for (int s = 0; s < toRemove; s++)
                    if (!spawner.TryRemoveRandom(_district)) break;
            }
        }

        private void CollectRenderers(GameObject go, bool tintNow)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;

            _renderers.AddRange(rs);

            if (!tintNow) return;
            _propBlock.SetColor(BaseColorId, _currentTint);
            foreach (var r in rs)
                if (r != null) r.SetPropertyBlock(_propBlock);
        }

        private void ApplyTinting(DistrictState state)
        {
            float health = (state.happiness + state.sustainability + state.infrastructure) / 3f;
            float roundedHealth = Mathf.Round(health);
            if (Mathf.Approximately(roundedHealth, _lastHealth)) return;
            _lastHealth = roundedHealth;

            _currentTint = ComputeTintColor(health);
            _propBlock.SetColor(BaseColorId, _currentTint);

            for (int i = _renderers.Count - 1; i >= 0; i--)
            {
                if (_renderers[i] == null) _renderers.RemoveAt(i);
                else _renderers[i].SetPropertyBlock(_propBlock);
            }
        }

        private static Color ComputeTintColor(float health)
        {
            if (health >= 70f)
                return Color.Lerp(Color.white, new Color(0.7f, 1f, 0.7f), (health - 70f) / 30f);
            if (health >= 40f) return Color.white;
            if (health >= 20f)
                return Color.Lerp(Color.white, new Color(1f, 1f, 0.6f), 1f - (health - 20f) / 20f);
            return Color.Lerp(new Color(1f, 1f, 0.6f), new Color(1f, 0.4f, 0.4f), 1f - health / 20f);
        }

        private static BuildingTier SelectTier(BuildingType type, DistrictState d)
        {
            if (type.Equals(BuildingType.Residential)) return TierFromMetric(d.population, 160f, 175f);
            if (type.Equals(BuildingType.Commercial)) return TierFromMetric(d.gdp, 55f, 75f);
            return TierFromMetric(d.infrastructure, 55f, 75f);
        }

        private static BuildingTier TierFromMetric(float value, float lowCut, float highCut)
        {
            if (value < lowCut) return BuildingTier.Low;
            if (value < highCut) return BuildingTier.Mid;
            return BuildingTier.High;
        }

        private static BuildingType NextType(int i) => (i % 3) switch
        {
            0 => BuildingType.Residential,
            1 => BuildingType.Commercial,
            _ => BuildingType.Industrial,
        };
    }
}
