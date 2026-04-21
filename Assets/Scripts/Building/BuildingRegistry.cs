// Author: Trevor Eilers

using System;
using System.Collections.Generic;
using UnityEngine;


namespace Building
{
    public readonly struct BuildingType : IEquatable<BuildingType>
    {
        private readonly string _type;

        public static BuildingType Residential = new BuildingType("Residential");
        public static BuildingType Commercial = new BuildingType("Commercial");
        public static BuildingType Industrial = new BuildingType("Industrial");

        private BuildingType(string type) => _type = type;

        public override string ToString() => _type;

        public byte Id => _type switch
        {
            "Residential" => 0,
            "Commercial" => 1,
            "Industrial" => 2,
            _ => 255,
        };

        public static BuildingType FromId(byte id) => id switch
        {
            0 => Residential,
            1 => Commercial,
            2 => Industrial,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown BuildingType id"),
        };

        public override bool Equals(object obj)
        {
            return _type == obj?.ToString();
        }

        public bool Equals(BuildingType other)
        {
            return _type == other._type;
        }

        public override int GetHashCode()
        {
            return (_type != null ? _type.GetHashCode() : 0);
        }
    }

    public readonly struct BuildingTier
    {
        private readonly string _tier;

        public static BuildingTier Low = new BuildingTier("Low");
        public static BuildingTier Mid = new BuildingTier("Mid");
        public static BuildingTier High = new BuildingTier("High");

        private BuildingTier(string tier) => _tier = tier;

        public override string ToString() => _tier;

        public byte Id => _tier switch
        {
            "Low" => 0,
            "Mid" => 1,
            "High" => 2,
            _ => 255,
        };

        public static BuildingTier FromId(byte id) => id switch
        {
            0 => Low,
            1 => Mid,
            2 => High,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown BuildingTier id"),
        };
    }

    public class BuildingRegistry : MonoBehaviour
    {
        private static readonly Dictionary<(string type, string tier), List<GameObject>> Buildings = new();

        void Start()
        {
            var rawBuildings = Resources.LoadAll<GameObject>("LowPolyMegapolis/Prefabs/Buildings");
            var plot = Resources.Load<GameObject>("Plot");

            if (!plot) throw new Exception("No Plot object found");

            if (!plot.TryGetComponent(out MeshRenderer plotRenderer))
                throw new Exception("No Plot MeshRenderer found");

            var plotBounds = plotRenderer.bounds;

            foreach (var building in rawBuildings)
            {
                if (!building.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    Debug.LogWarning($"{building.name} has no MeshRenderer");
                    continue;
                }

                var buildingBounds = meshRenderer.bounds;
                if (!FitsInside(buildingBounds, plotBounds))
                {
                    float scaleX = plotBounds.size.x / buildingBounds.size.x;
                    float scaleZ = plotBounds.size.z / buildingBounds.size.z;
                    building.transform.localScale *= Mathf.Min(scaleX, scaleZ);
                }

                var rawTag = building.tag;
                var tags = rawTag.Split('_');
                if (tags.Length != 2) continue;

                var key = (tags[0], tags[1]);
                if (!Buildings.TryGetValue(key, out var list))
                {
                    list = new List<GameObject>();
                    Buildings.Add(key, list);
                }
                list.Add(building);
            }

            // Sort each list by GameObject.name for deterministic cross-client indexing.
            foreach (var list in Buildings.Values)
                list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        public static List<GameObject> Get(BuildingType type, BuildingTier tier)
        {
            return !Buildings.ContainsKey((type.ToString(), tier.ToString()))
                ? null
                : Buildings[(type.ToString(), tier.ToString())];
        }

        public static GameObject GetAt(BuildingType type, BuildingTier tier, int index)
        {
            var list = Get(type, tier);
            if (list == null || index < 0 || index >= list.Count) return null;
            return list[index];
        }

        public static int Count(BuildingType type, BuildingTier tier)
        {
            var list = Get(type, tier);
            return list?.Count ?? 0;
        }

        private bool FitsInside(Bounds a, Bounds b)
        {
            bool fitsX = a.min.x >= b.min.x && a.max.x <= b.max.x;
            bool fitsZ = a.min.z >= b.min.z && a.max.z <= b.max.z;

            return fitsX && fitsZ;
        }
    }
}
