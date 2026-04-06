using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Building
{
    public readonly struct BuildingType
    {
        private readonly string _type;
        
        public static BuildingType Commercial = new BuildingType("Commercial");
        public static BuildingType Industrial = new BuildingType("Industrial");
        public static BuildingType Residential = new BuildingType("Residential");
        
        private BuildingType(string type) => _type = type;
        
        public override string ToString() => _type;
    }

    public readonly struct BuildingTier
    {
        private readonly string _tier;

        public static BuildingTier Low = new BuildingTier("Low");
        public static BuildingTier Med = new BuildingTier("Med");
        public static BuildingTier High = new BuildingTier("High");
        
        private BuildingTier(string tier) => _tier = tier;
        
        public override string ToString() => _tier;
    }
    
    public class BuildingRegistry : MonoBehaviour
    {
        private static Object[] _buildingObjects;
        private static readonly Dictionary<(string type, string tier), HashSet<GameObject>> Buildings = new();
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _buildingObjects = Resources.LoadAll("LowPolyMegapolis/Prefabs/Buildings");
            foreach (var buildingObject in _buildingObjects)
            {
                if (buildingObject is not GameObject building) continue;

                var rawTag = building.tag;
                var tags = rawTag.Split('_');

                if (Buildings.ContainsKey((tags[0], tags[1])))
                {
                    var set = Buildings[(tags[0], tags[1])];
                    set.Add(building);
                }
                else
                {
                    Buildings.Add((tags[0], tags[1]), new HashSet<GameObject>());
                }
            }
        }
        
        public static HashSet<GameObject> Get(BuildingType type, BuildingTier tier)
        {
            return !Buildings.ContainsKey((type.ToString(), tier.ToString())) 
                ? null 
                : Buildings[(type.ToString(), tier.ToString())];
        }
    }
}
