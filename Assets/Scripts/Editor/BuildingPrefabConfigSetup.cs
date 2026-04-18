using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuildingPrefabConfigSetup
{
    private const string ConfigPath = "Assets/Settings/BuildingPrefabs.asset";
    private const string BuildingsRoot = "Assets/Prefabs/Buildings";

    [MenuItem("Tools/Setup Building Prefabs")]
    public static void Populate()
    {
        var config = AssetDatabase.LoadAssetAtPath<BuildingPrefabConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<BuildingPrefabConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        config.residential = new CategoryTiers();
        config.commercial = new CategoryTiers();
        config.industrial = new CategoryTiers();
        config.civic = new CategoryTiers();

        var buckets = new Dictionary<(BuildingCategory, int), List<GameObject>>();
        var globals = new List<GameObject>[3] { new(), new(), new() };

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { BuildingsRoot });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int tier = ClassifyTier(prefab.name);
            if (tier < 0) continue;

            globals[tier].Add(prefab);

            if (TryClassifyCategory(prefab.name, out var cat))
            {
                var key = (cat, tier);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<GameObject>();
                    buckets[key] = list;
                }
                list.Add(prefab);
            }
        }

        AssignCategory(config.residential, buckets, BuildingCategory.Residential);
        AssignCategory(config.commercial, buckets, BuildingCategory.Commercial);
        AssignCategory(config.industrial, buckets, BuildingCategory.Industrial);
        AssignCategory(config.civic, buckets, BuildingCategory.Civic);

        config.globalLow = globals[0].ToArray();
        config.globalMid = globals[1].ToArray();
        config.globalHigh = globals[2].ToArray();

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log($"BuildingPrefabs populated — global: {globals[0].Count}/{globals[1].Count}/{globals[2].Count} (low/mid/high)");
        foreach (BuildingCategory cat in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            int l = buckets.TryGetValue((cat, 0), out var a) ? a.Count : 0;
            int m = buckets.TryGetValue((cat, 1), out var b) ? b.Count : 0;
            int h = buckets.TryGetValue((cat, 2), out var c) ? c.Count : 0;
            Debug.Log($"  {cat}: {l}/{m}/{h}");
        }
    }

    private static void AssignCategory(CategoryTiers tiers,
        Dictionary<(BuildingCategory, int), List<GameObject>> buckets, BuildingCategory cat)
    {
        tiers.low = buckets.TryGetValue((cat, 0), out var l) ? l.ToArray() : System.Array.Empty<GameObject>();
        tiers.mid = buckets.TryGetValue((cat, 1), out var m) ? m.ToArray() : System.Array.Empty<GameObject>();
        tiers.high = buckets.TryGetValue((cat, 2), out var h) ? h.ToArray() : System.Array.Empty<GameObject>();
    }

    private static int ClassifyTier(string name)
    {
        if (name.StartsWith("SmallHouse") || name.StartsWith("OldHouse") ||
            name.StartsWith("Donuts") || name.StartsWith("Burgers") ||
            name.StartsWith("Pizza") || name.StartsWith("SmallMarket") ||
            name.StartsWith("Gas_Station") || name == "Factory_01" ||
            name.StartsWith("Construction") || name.StartsWith("ParkingZone") ||
            name.StartsWith("ParkingBox") || name.StartsWith("Church") ||
            name.StartsWith("BasketballPlayground") || name.StartsWith("FootballPlayground"))
            return 0;

        if (name.StartsWith("SuburbHouse") || name.StartsWith("Cinema") ||
            name.StartsWith("Airport") || name.StartsWith("CityHall") ||
            name.StartsWith("Hospital") ||
            name == "Factory_04" || name == "Factory_05")
            return 2;

        if (name.StartsWith("House") || name.StartsWith("Cafe") ||
            name.StartsWith("Coffe") || name.StartsWith("Bakery") ||
            name.StartsWith("Sushi") || name.StartsWith("Supermarket") ||
            name.StartsWith("Bank") || name.StartsWith("Hotel") ||
            name.StartsWith("AutoService") || name.StartsWith("Station") ||
            name.StartsWith("Office") || name.StartsWith("FireStation") ||
            name.StartsWith("Police") ||
            name == "Factory_02" || name == "Factory_03")
            return 1;

        return -1;
    }

    private static bool TryClassifyCategory(string name, out BuildingCategory cat)
    {
        if (name.StartsWith("SmallHouse") || name.StartsWith("OldHouse") ||
            name.StartsWith("SuburbHouse") || name.StartsWith("House"))
        {
            cat = BuildingCategory.Residential;
            return true;
        }

        if (name.StartsWith("Cafe") || name.StartsWith("Coffe") ||
            name.StartsWith("Bakery") || name.StartsWith("Sushi") ||
            name.StartsWith("Supermarket") || name.StartsWith("SmallMarket") ||
            name.StartsWith("Donuts") || name.StartsWith("Burgers") ||
            name.StartsWith("Pizza") || name.StartsWith("Bank") ||
            name.StartsWith("Hotel") || name.StartsWith("Cinema") ||
            name.StartsWith("AutoService") || name.StartsWith("Gas_Station") ||
            name.StartsWith("Office"))
        {
            cat = BuildingCategory.Commercial;
            return true;
        }

        if (name.StartsWith("Factory") || name.StartsWith("Construction") ||
            name.StartsWith("ParkingZone") || name.StartsWith("ParkingBox") ||
            name.StartsWith("Station") || name.StartsWith("Airport"))
        {
            cat = BuildingCategory.Industrial;
            return true;
        }

        if (name.StartsWith("Church") || name.StartsWith("CityHall") ||
            name.StartsWith("Hospital") || name.StartsWith("Police") ||
            name.StartsWith("FireStation") ||
            name.StartsWith("BasketballPlayground") || name.StartsWith("FootballPlayground"))
        {
            cat = BuildingCategory.Civic;
            return true;
        }

        cat = default;
        return false;
    }
}
