using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuildingPrefabConfigSetup
{
    [MenuItem("Tools/Setup Building Prefabs")]
    public static void Populate()
    {
        // Find or create the config asset
        var config = AssetDatabase.LoadAssetAtPath<BuildingPrefabConfig>("Assets/Settings/BuildingPrefabs.asset");
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<BuildingPrefabConfig>();
            AssetDatabase.CreateAsset(config, "Assets/Settings/BuildingPrefabs.asset");
        }

        var low = new List<GameObject>();
        var mid = new List<GameObject>();
        var high = new List<GameObject>();

        // Load all prefabs from the buildings folder
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/LowPolyMegapolis/Prefabs/Buildings" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            string name = prefab.name;
            int tier = Classify(name);

            switch (tier)
            {
                case 0: low.Add(prefab); break;
                case 1: mid.Add(prefab); break;
                case 2: high.Add(prefab); break;
            }
        }

        config.lowTier = low.ToArray();
        config.midTier = mid.ToArray();
        config.highTier = high.ToArray();

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log($"BuildingPrefabConfig populated: {low.Count} low, {mid.Count} mid, {high.Count} high tier prefabs");
    }

    private static int Classify(string name)
    {
        // Low tier
        if (name.StartsWith("SmallHouse") || name.StartsWith("OldHouse") ||
            name.StartsWith("Donuts") || name.StartsWith("Burgers") ||
            name.StartsWith("Pizza") || name.StartsWith("SmallMarket") ||
            name.StartsWith("Gas_Station") || name == "Factory_01" ||
            name.StartsWith("Construction") || name.StartsWith("ParkingZone") ||
            name.StartsWith("ParkingBox") || name.StartsWith("Church") ||
            name.StartsWith("BasketballPlayground") || name.StartsWith("FootballPlayground"))
            return 0;

        // High tier
        if (name.StartsWith("SuburbHouse") || name.StartsWith("Cinema") ||
            name.StartsWith("Airport") || name.StartsWith("CityHall") ||
            name.StartsWith("Hospital") ||
            name == "Factory_04" || name == "Factory_05")
            return 2;

        // Mid tier
        if (name.StartsWith("House") || name.StartsWith("Cafe") ||
            name.StartsWith("Coffe") || name.StartsWith("Bakery") ||
            name.StartsWith("Sushi") || name.StartsWith("Supermarket") ||
            name.StartsWith("Bank") || name.StartsWith("Hotel") ||
            name.StartsWith("AutoService") || name.StartsWith("Station") ||
            name.StartsWith("Office") || name.StartsWith("FireStation") ||
            name.StartsWith("Police") ||
            name == "Factory_02" || name == "Factory_03")
            return 1;

        return -1; // skip
    }
}
