using UnityEngine;

[CreateAssetMenu(fileName = "BuildingPrefabs", menuName = "CivicEngine/Building Prefab Config")]
public class BuildingPrefabConfig : ScriptableObject
{
    [Header("Low Tier — SmallHouse, OldHouse, Church, Gas Station, etc.")]
    public GameObject[] lowTier;

    [Header("Mid Tier — House, Hotel, Bank, Cafe, Police, FireStation, etc.")]
    public GameObject[] midTier;

    [Header("High Tier — SuburbHouse, CityHall, Hospital, Cinema, Airport, etc.")]
    public GameObject[] highTier;
}
