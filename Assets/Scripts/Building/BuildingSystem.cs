using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Building
{
    public class BuildingSystem : MonoBehaviour
    {
        private BuildingRegistry _registry;

        [SerializeField]
        private PlotGrid[] plots;

        private void Start()
        {
            _registry = GetComponent<BuildingRegistry>();
        }

        public void TrySpawn(BuildingType type, BuildingTier tier)
        {
            var buildings = BuildingRegistry.Get(type, tier);
            
            
        }
        
        public static Bounds GetBounds(GameObject prefab)
        {
            var mesh = prefab.GetComponent<Mesh>();
            var bounds = mesh.bounds;
            return bounds;
        }
    }
}
