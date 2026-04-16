using System;
using Building;
using UnityEngine;

public class DistrictSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnSlot
    {
        public string name;        // R1, R2, etc (just for you)
        public Transform point;    // empty object in scene
        public GameObject prefab;  // building for that slot
    }

    [Header("Residential (R1-R20)")]
    public SpawnSlot[] residentialSlots;

    [Header("Industrial (I1-I9)")]
    public SpawnSlot[] industrialSlots;

    [Header("Commercial (C1-C13)")]
    public SpawnSlot[] commercialSlots;

    public DistrictPlot districtPlot;

    private void Start()
    {
        
    }

    // ---------------- BATCHES ----------------

    public void SpawnBatch1()
    {
        SpawnRange(residentialSlots, 0, 7);   // R1–R8
        SpawnRange(industrialSlots, 0, 3);    // I1–I4
        SpawnRange(commercialSlots, 0, 4);    // C1–C5
    }

    public void SpawnBatch2()
    {
        SpawnRange(residentialSlots, 8, 15);  // R9–R16
        SpawnRange(industrialSlots, 4, 5);    // I5–I6
        SpawnRange(commercialSlots, 5, 8);    // C6–C9
    }

    public void SpawnBatch3()
    {
        SpawnRange(residentialSlots, 16, 19); // R17–R20
        SpawnRange(industrialSlots, 6, 8);    // I7–I9
        SpawnRange(commercialSlots, 9, 12);   // C10–C13
    }

    // ---------------- HELPER ----------------

    void SpawnRange(SpawnSlot[] slots, int start, int end)
    {
        if (slots == null) return;
        
        for (int i = start; i <= end && i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (slots[i].point == null) continue;
            if (slots[i].prefab == null) continue;
        
            Instantiate(slots[i].prefab, slots[i].point.position, slots[i].point.rotation);
        }
    }
    
    public void TrySpawnRandom(BuildingType type, BuildingTier tier)
    {
        var buildings = BuildingRegistry.Get(type, tier);
        int randBuildingIndex = UnityEngine.Random.Range(0, buildings.Count);
        int randPlotIndex = UnityEngine.Random.Range(0, districtPlot.UnoccupiedIndices.Count);
        districtPlot.Add(randPlotIndex, buildings[randBuildingIndex]);
    }
}