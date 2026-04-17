using System;
using System.Collections.Generic;
using UnityEngine;

namespace Building
{
    [System.Serializable]
    public class SpawnSlot
    {
        public string name;        // R1, R2, etc (just for you)
        public Transform point;    // empty object in scene
        public GameObject prefab;  // building for that slot
    }
    
    public class BuildingSpawner : MonoBehaviour
    {
        private readonly List<DistrictSquare> _residentialSquares =  new();
        private readonly List<DistrictSquare> _commercialSquares = new();
        private readonly List<DistrictSquare> _industrialSquares = new();

        private void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                switch (transform.GetChild(i).tag)
                {
                    case "Residential":
                    {
                        _residentialSquares.Add(transform.GetChild(i).GetComponent<DistrictSquare>());
                        break;
                    }
                    case "Commercial":
                    {
                        _commercialSquares.Add(transform.GetChild(i).GetComponent<DistrictSquare>());
                        break;
                    }
                    case "Industrial":
                    {
                        _industrialSquares.Add(transform.GetChild(i).GetComponent<DistrictSquare>());
                        break;
                    }
                }
            }
        }

        public void TrySpawnRandom(BuildingType type, BuildingTier tier)
        {
            var squares =
                type.Equals(BuildingType.Residential) ? _residentialSquares :
                type.Equals(BuildingType.Commercial)  ? _commercialSquares  :
                                                        _industrialSquares;

            if (squares.Count == 0) return;

            var buildings = BuildingRegistry.Get(type, tier);
            if (buildings == null || buildings.Count == 0) return;

            var districtSquare = squares[UnityEngine.Random.Range(0, squares.Count)];
            var prefab = buildings[UnityEngine.Random.Range(0, buildings.Count)];
            districtSquare.TryAddRandom(prefab);
        }
    }
}