using UnityEngine;
using UnityEngine.InputSystem;

namespace Building
{
    public class BuildingSystem : MonoBehaviour
    {
        public BuildingSpawner _spawner;

        private float _residentialSpawnChance = 0.5f;
        private float _commercialSpawnChance = 0.5f;
        private float _industrialSpawnChance = 0.5f;

        private float _elapsedTime = 0;
        private float _spawnTime = 1;
        
        void Update()
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime < _spawnTime) return;
            _elapsedTime = 0;

            var r_rand = Random.value;
            var c_rand = Random.value;
            var i_rand = Random.value;

            if (_residentialSpawnChance >= r_rand) 
                _spawner.TrySpawnRandom(BuildingType.Residential, BuildingTier.Low);
            if (_commercialSpawnChance >= c_rand)
                _spawner.TrySpawnRandom(BuildingType.Commercial, BuildingTier.Low);
            if (_industrialSpawnChance >= i_rand) 
                _spawner.TrySpawnRandom(BuildingType.Industrial, BuildingTier.Low);
        }
    }
}
