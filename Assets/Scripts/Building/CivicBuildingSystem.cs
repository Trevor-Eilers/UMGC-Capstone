// Author: Trevor Eilers

using Simulation;
using Unity.Netcode;
using UnityEngine;

namespace Building
{
    public class CivicBuildingSystem : MonoBehaviour
    {
        public BuildingSpawner spawner;

        // Tuning. At default policies (4 players, cityContribution=25, gdp=50)
        // aggregate = 4 * 25 * 50 = 5000 (Mid tier, ~10 plots).
        // At max policies (4 players, cityContribution=50, gdp=100)
        // aggregate = 4 * 50 * 100 = 20000 (High tier, ~40 plots).
        private const float CivicPerPlot = 500f;
        private const float CivicTierLowCut = 3000f;
        private const float CivicTierHighCut = 10000f;
        private const int MaxBuildingsPerTick = 3;

        private NetworkList<BuildingPlacement> _placements;

        public NetworkList<BuildingPlacement> Placements
        {
            get => _placements;
            set
            {
                if (_placements == value) return;
                Unsubscribe();
                _placements = value;
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (_placements == null || spawner == null) return;
            _placements.OnListChanged += OnPlacementsChanged;

            for (int i = 0; i < _placements.Count; i++)
                spawner.ApplyPlacement(_placements[i]);
        }

        private void Unsubscribe()
        {
            if (_placements == null) return;
            _placements.OnListChanged -= OnPlacementsChanged;
        }

        private void OnDestroy() => Unsubscribe();

        private void OnPlacementsChanged(NetworkListEvent<BuildingPlacement> evt)
        {
            switch (evt.Type)
            {
                case NetworkListEvent<BuildingPlacement>.EventType.Add:
                    spawner.ApplyPlacement(evt.Value);
                    break;
                case NetworkListEvent<BuildingPlacement>.EventType.RemoveAt:
                case NetworkListEvent<BuildingPlacement>.EventType.Remove:
                    spawner.RevertPlacement(evt.Value);
                    break;
                case NetworkListEvent<BuildingPlacement>.EventType.Clear:
                    break;
            }
        }

        public void UpdateCenter(DistrictState[] districtStates)
        {
            if (spawner == null || _placements == null) return;

            int totalPlots = spawner.TotalPlotCount();
            if (totalPlots == 0) return;

            float aggregate = 0f;
            for (int i = 0; i < districtStates.Length; i++)
                aggregate += districtStates[i].policyValues.cityContribution * districtStates[i].gdp;

            int targetOccupied = Mathf.Clamp(
                Mathf.RoundToInt(aggregate / CivicPerPlot), 0, totalPlots);
            int currentOccupied = _placements.Count;
            var tier = SelectTier(aggregate);

            if (currentOccupied < targetOccupied)
            {
                var max = Random.Range(1, MaxBuildingsPerTick);
                int toSpawn = Mathf.Min(targetOccupied - currentOccupied, max);
                for (int s = 0; s < toSpawn; s++)
                    if (!spawner.TrySpawnRandom(_placements, BuildingType.Civic, tier)) break;
            }
            else if (currentOccupied > targetOccupied)
            {
                var max = Random.Range(1, MaxBuildingsPerTick);
                int toRemove = Mathf.Min(currentOccupied - targetOccupied, max);
                for (int s = 0; s < toRemove; s++)
                    if (!spawner.TryRemoveRandom(_placements)) break;
            }
        }

        private static BuildingTier SelectTier(float aggregate)
        {
            if (aggregate < CivicTierLowCut) return BuildingTier.Low;
            if (aggregate < CivicTierHighCut) return BuildingTier.Mid;
            return BuildingTier.High;
        }
    }
}
