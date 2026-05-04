// Author: Trevor Eilers

using System.Collections.Generic;
using System.Linq;
using Core;
using Unity.Netcode;
using UnityEngine;

namespace Building
{
    public class BuildingSpawner : MonoBehaviour
    {
        private readonly List<DistrictSquare> _residentialSquares = new();
        private readonly List<DistrictSquare> _commercialSquares = new();
        private readonly List<DistrictSquare> _industrialSquares = new();
        private readonly List<DistrictSquare> _civicSquares = new();

        private void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                switch (transform.GetChild(i).tag)
                {
                    case "Residential":
                        _residentialSquares.Add(transform.GetChild(i).GetComponent<DistrictSquare>());
                        break;
                    case "Commercial":
                        _commercialSquares.Add(transform.GetChild(i).GetComponent<DistrictSquare>());
                        break;
                    case "Industrial":
                        _industrialSquares.Add(transform.GetChild(i).GetComponent<DistrictSquare>());
                        break;
                    case "Civic":
                        _civicSquares.Add(transform.GetChild(i).GetComponent<DistrictSquare>());
                        break;
                }
            }
        }

        public bool TrySpawnRandom(NetworkList<BuildingPlacement> placements, BuildingType type, BuildingTier tier)
        {
            if (placements == null) return false;

            var squares = SquaresFor(type);
            if (squares.Count == 0) return false;

            int prefabCount = BuildingRegistry.Count(type, tier);
            if (prefabCount == 0) return false;

            for (int attempt = 0; attempt < squares.Count; attempt++)
            {
                int si = Random.Range(0, squares.Count);
                var square = squares[si];
                if (square.OccupiedCount >= square.PlotCount) continue;

                int plotIndex = square.UnoccupiedIndices.ElementAt(
                    Random.Range(0, square.UnoccupiedIndices.Count));
                int prefabIndex = Random.Range(0, prefabCount);

                placements.Add(new BuildingPlacement
                {
                    typeId = type.Id,
                    tierId = tier.Id,
                    squareIndex = (byte)si,
                    plotIndex = (byte)plotIndex,
                    prefabIndex = (ushort)prefabIndex,
                });
                return true;
            }
            return false;
        }

        public bool TryRemoveRandom(NetworkList<BuildingPlacement> placements)
        {
            if (placements == null || placements.Count == 0) return false;
            int victim = Random.Range(0, placements.Count);
            placements.RemoveAt(victim);
            return true;
        }

        public GameObject ApplyPlacement(BuildingPlacement p)
        {
            var squares = SquaresFor(BuildingType.FromId(p.typeId));
            if (p.squareIndex >= squares.Count) return null;

            var prefab = BuildingRegistry.GetAt(
                BuildingType.FromId(p.typeId),
                BuildingTier.FromId(p.tierId),
                p.prefabIndex);
            if (prefab == null) return null;

            return squares[p.squareIndex].Add(p.plotIndex, prefab);
        }

        public void RevertPlacement(BuildingPlacement p)
        {
            var squares = SquaresFor(BuildingType.FromId(p.typeId));
            if (p.squareIndex >= squares.Count) return;
            squares[p.squareIndex].RemoveAt(p.plotIndex);
        }

        public int TotalPlotCount()
        {
            int total = 0;
            foreach (var s in _residentialSquares) total += s.PlotCount;
            foreach (var s in _commercialSquares) total += s.PlotCount;
            foreach (var s in _industrialSquares) total += s.PlotCount;
            foreach (var s in _civicSquares) total += s.PlotCount;
            return total;
        }

        public int TotalOccupied()
        {
            int total = 0;
            foreach (var s in _residentialSquares) total += s.OccupiedCount;
            foreach (var s in _commercialSquares) total += s.OccupiedCount;
            foreach (var s in _industrialSquares) total += s.OccupiedCount;
            foreach (var s in _civicSquares) total += s.OccupiedCount;
            return total;
        }

        private List<DistrictSquare> SquaresFor(BuildingType type)
        {
            if (type.Equals(BuildingType.Residential)) return _residentialSquares;
            if (type.Equals(BuildingType.Commercial)) return _commercialSquares;
            if (type.Equals(BuildingType.Civic)) return _civicSquares;
            return _industrialSquares;
        }
    }
}
