using System;
using UnityEngine;

public enum BuildingCategory
{
    Residential,
    Commercial,
    Industrial,
    Civic
}

[Serializable]
public class CategoryTiers
{
    public GameObject[] low;
    public GameObject[] mid;
    public GameObject[] high;
}

[CreateAssetMenu(fileName = "BuildingPrefabs", menuName = "CivicEngine/Building Prefab Config")]
public class BuildingPrefabConfig : ScriptableObject
{
    [Header("Residential — SmallHouse, OldHouse, House, SuburbHouse")]
    public CategoryTiers residential;

    [Header("Commercial — Cafe, Bank, Hotel, Office, Supermarket, Cinema, etc.")]
    public CategoryTiers commercial;

    [Header("Industrial — Factory, Construction, Parking, Station, Airport")]
    public CategoryTiers industrial;

    [Header("Civic — Church, CityHall, Hospital, Police, FireStation, Playgrounds")]
    public CategoryTiers civic;

    [Header("Global Fallback — used when a category/tier is empty")]
    public GameObject[] globalLow;
    public GameObject[] globalMid;
    public GameObject[] globalHigh;

    public GameObject[] GetTier(BuildingCategory category, int tier)
    {
        CategoryTiers cats = category switch
        {
            BuildingCategory.Residential => residential,
            BuildingCategory.Commercial => commercial,
            BuildingCategory.Industrial => industrial,
            BuildingCategory.Civic => civic,
            _ => null
        };

        GameObject[] primary = null;
        if (cats != null)
        {
            primary = tier switch
            {
                0 => cats.low,
                1 => cats.mid,
                2 => cats.high,
                _ => null
            };
        }
        if (primary != null && primary.Length > 0) return primary;

        GameObject[] fallback = tier switch
        {
            0 => globalLow,
            1 => globalMid,
            2 => globalHigh,
            _ => null
        };
        if (fallback != null && fallback.Length > 0) return fallback;

        if (globalMid != null && globalMid.Length > 0) return globalMid;
        if (globalLow != null && globalLow.Length > 0) return globalLow;
        if (globalHigh != null && globalHigh.Length > 0) return globalHigh;
        return null;
    }
}