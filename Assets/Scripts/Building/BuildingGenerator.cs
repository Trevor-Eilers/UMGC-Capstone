using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[ExecuteInEditMode]
public class BuildingGenerator : NetworkBehaviour
{
    private GameObject _groundPlane;
    [SerializeField] private BuildingPrefabConfig prefabConfig;

    private const float POP_PER_BUILDING = 12f;
    private const int MAX_BUILDINGS_PER_DISTRICT = 50;
    private const int BUILDINGS_PER_TICK = 10;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly float[] ValidRotations = { 0f, 90f, 180f, 270f };

    private struct BuildingSlot
    {
        public Vector3 position;
        public GameObject forestTile;
        public GameObject building;
        public bool hasBuilding;
    }

    private List<BuildingSlot>[] _districtSlots;
    private int[] _currentBuildingCount;
    private Vector3 _mapCenter;
    private Bounds _mapBounds;
    private MaterialPropertyBlock _propBlock;
    private List<Renderer>[] _districtRenderers;
    private float[] _lastHealth;

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _districtSlots = new List<BuildingSlot>[4];
        _currentBuildingCount = new int[4];

        _districtRenderers = new List<Renderer>[4];
        _lastHealth = new float[] { -1f, -1f, -1f, -1f };
        for (int i = 0; i < 4; i++)
        {
            _districtSlots[i] = new List<BuildingSlot>();
            _districtRenderers[i] = new List<Renderer>();
        }

        ComputeMapCenter();
        CreateGroundPlane();
        CollectSlots();
        SortSlotsByDistanceFromCenter();

        for (int i = 0; i < 4; i++)
            Debug.Log($"BuildingGenerator District {i}: {_districtSlots[i].Count} slots, {_currentBuildingCount[i]} buildings");
    }

    private void ComputeMapCenter()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            _mapCenter = transform.position;
            _mapBounds = new Bounds(transform.position, Vector3.one * 500f);
            return;
        }
        _mapBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            _mapBounds.Encapsulate(renderers[i].bounds);
        _mapCenter = _mapBounds.center;
    }

    private void CreateGroundPlane()
    {
        // Check if ground already exists
        _groundPlane = transform.Find("CityGround")?.gameObject;
        if (_groundPlane != null) return;

        _groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        _groundPlane.name = "CityGround";
        _groundPlane.transform.SetParent(transform);
        _groundPlane.transform.position = new Vector3(_mapCenter.x, -0.05f, _mapCenter.z);

        float scaleX = (_mapBounds.size.x + 100f) / 10f;
        float scaleZ = (_mapBounds.size.z + 100f) / 10f;
        _groundPlane.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

        var renderer = _groundPlane.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.35f, 0.50f, 0.25f);
        renderer.material = mat;

        var collider = _groundPlane.GetComponent<Collider>();
        if (collider != null) DestroyImmediate(collider);
    }

    void OnEnable()
    {
        // In edit mode, just create the ground plane
        if (!Application.isPlaying)
        {
            ComputeMapCenter();
            CreateGroundPlane();
        }
    }

    void OnDisable()
    {
        // Clean up in edit mode to avoid duplicates
        if (!Application.isPlaying)
        {
            var existing = transform.Find("CityGround")?.gameObject;
            if (existing != null) DestroyImmediate(existing);
        }
    }

    private List<Bounds> _roadBounds = new();
    private Bounds _cityGridBounds;

    private void CollectSlots()
    {
        // First collect existing buildings to determine valid city grid area
        CollectExistingBuildings(transform);

        // Compute city grid bounds from existing building positions
        ComputeCityGridBounds();

        // Collect road bounds for proximity filtering
        CollectRoadBounds(transform);
        Debug.Log($"BuildingGenerator: collected {_roadBounds.Count} road bounds, grid={_cityGridBounds}");

        // Collect Forest Tiles as available building slots (filtered by grid + road proximity)
        CollectForestTiles(transform);
    }

    private void ComputeCityGridBounds()
    {
        // Use existing hand-placed building positions to define valid city area
        bool first = true;
        for (int d = 0; d < 4; d++)
        {
            foreach (var slot in _districtSlots[d])
            {
                if (slot.hasBuilding)
                {
                    if (first)
                    {
                        _cityGridBounds = new Bounds(slot.position, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        _cityGridBounds.Encapsulate(slot.position);
                    }
                }
            }
        }
        // Expand slightly to include nearby Forest Tiles
        _cityGridBounds.Expand(20f);
    }

    private void CollectRoadBounds(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            string name = child.gameObject.name;

            // Collect renderer bounds from road meshes
            if (name.StartsWith("MobileRoad") || name.Contains("Road"))
            {
                var renderers = child.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                    _roadBounds.Add(r.bounds);
            }

            // Recurse into containers
            if (name.StartsWith("District") || name.StartsWith("road stretch") || name.StartsWith("road"))
                CollectRoadBounds(child);
        }
    }

    private bool IsTooCloseToRoad(Vector3 pos)
    {
        const float MIN_ROAD_DISTANCE = 5f;
        float sqrThreshold = MIN_ROAD_DISTANCE * MIN_ROAD_DISTANCE;
        foreach (var roadBound in _roadBounds)
        {
            if (roadBound.SqrDistance(pos) < sqrThreshold)
                return true;
        }
        return false;
    }

    private void CollectForestTiles(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            string name = child.gameObject.name;

            if (name.StartsWith("Forest Tile"))
            {
                // Skip tiles outside the city grid
                if (!_cityGridBounds.Contains(child.position)) continue;

                // Skip tiles that overlap with roads
                if (IsTooCloseToRoad(child.position)) continue;

                int district = GetDistrict(child.position);
                _districtSlots[district].Add(new BuildingSlot
                {
                    position = child.position,
                    forestTile = child.gameObject,
                    building = null,
                    hasBuilding = false
                });
            }
            else if (name.StartsWith("District") || name.StartsWith("road"))
            {
                // Recurse into District/road containers to find nested Forest Tiles
                CollectForestTiles(child);
            }
        }
    }

    private void CollectExistingBuildings(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            string name = child.gameObject.name;

            if (IsBuilding(name))
            {
                int district = GetDistrict(child.position);
                _districtSlots[district].Add(new BuildingSlot
                {
                    position = child.position,
                    forestTile = null,
                    building = child.gameObject,
                    hasBuilding = true
                });
                _currentBuildingCount[district]++;
            }
            else if (!name.StartsWith("Forest Tile") && !name.StartsWith("Plane")
                     && !name.StartsWith("CityGround"))
            {
                CollectExistingBuildings(child);
            }
        }
    }

    private static bool IsBuilding(string name)
    {
        return name.StartsWith("SmallHouse") || name.StartsWith("OldHouse") ||
               name.StartsWith("SuburbHouse") || name.StartsWith("House") ||
               name.StartsWith("Factory") || name.StartsWith("Hotel") ||
               name.StartsWith("Bank") || name.StartsWith("Church") ||
               name.StartsWith("CityHall") || name.StartsWith("Hospital") ||
               name.StartsWith("Police") || name.StartsWith("FireStation") ||
               name.StartsWith("Cafe") || name.StartsWith("Coffe") ||
               name.StartsWith("Bakery") || name.StartsWith("Sushi") ||
               name.StartsWith("Burgers") || name.StartsWith("Donuts") ||
               name.StartsWith("Cinema") || name.StartsWith("Airport") ||
               name.StartsWith("Office") || name.StartsWith("Station") ||
               name.StartsWith("Supermarket") || name.StartsWith("SmallMarket") ||
               name.StartsWith("Gas_Station") || name.StartsWith("AutoService") ||
               name.StartsWith("Construction") || name.StartsWith("ParkingZone") ||
               name.StartsWith("ParkingBox") || name.StartsWith("BasketballPlayground") ||
               name.StartsWith("FootballPlayground");
    }

    private void SortSlotsByDistanceFromCenter()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 districtCenter = GetDistrictCenter(i);
            _districtSlots[i].Sort((a, b) =>
            {
                float distA = Vector3.Distance(a.position, districtCenter);
                float distB = Vector3.Distance(b.position, districtCenter);
                return distA.CompareTo(distB);
            });
        }
    }

    private Vector3 GetDistrictCenter(int district)
    {
        float offsetX = 50f;
        float offsetZ = 50f;
        return district switch
        {
            0 => _mapCenter + new Vector3(-offsetX, 0, offsetZ),
            1 => _mapCenter + new Vector3(offsetX, 0, offsetZ),
            2 => _mapCenter + new Vector3(-offsetX, 0, -offsetZ),
            3 => _mapCenter + new Vector3(offsetX, 0, -offsetZ),
            _ => _mapCenter
        };
    }

    private int GetDistrict(Vector3 pos)
    {
        bool north = pos.z >= _mapCenter.z;
        bool west = pos.x <= _mapCenter.x;
        if (north && west) return 0;
        if (north && !west) return 1;
        if (!north && west) return 2;
        return 3;
    }

    public void UpdateDistrict(int districtIndex, DistrictState district)
    {
        if (districtIndex < 0 || districtIndex >= 4) return;
        if (prefabConfig == null) return;

        int targetBuildings = Mathf.Clamp(
            Mathf.RoundToInt(district.population / POP_PER_BUILDING),
            0, MAX_BUILDINGS_PER_DISTRICT);

        int current = _currentBuildingCount[districtIndex];

        if (targetBuildings > current)
        {
            int toSpawn = Mathf.Min(targetBuildings - current, BUILDINGS_PER_TICK);
            for (int s = 0; s < toSpawn; s++)
                SpawnBuilding(districtIndex, district.gdp);
        }
        else if (targetBuildings < current)
        {
            int toRemove = Mathf.Min(current - targetBuildings, BUILDINGS_PER_TICK);
            for (int s = 0; s < toRemove; s++)
                RemoveBuilding(districtIndex);
        }

        ApplyTinting(districtIndex, district);
    }

    private void SpawnBuilding(int districtIndex, float gdp)
    {
        var slots = _districtSlots[districtIndex];

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (!slot.hasBuilding)
            {
                // Hide forest tile if it exists
                if (slot.forestTile != null)
                    slot.forestTile.SetActive(false);

                GameObject prefab = PickPrefab(gdp);
                if (prefab == null) return;

                Quaternion rot = Quaternion.Euler(0, ValidRotations[Random.Range(0, ValidRotations.Length)], 0);
                slot.building = Instantiate(prefab, slot.position, rot, transform);
                slot.hasBuilding = true;
                slots[i] = slot;
                _currentBuildingCount[districtIndex]++;
                _districtRenderers[districtIndex].AddRange(slot.building.GetComponentsInChildren<Renderer>());
                _lastHealth[districtIndex] = -1f; // force tint update
                return;
            }
        }
    }

    private void RemoveBuilding(int districtIndex)
    {
        var slots = _districtSlots[districtIndex];

        for (int i = slots.Count - 1; i >= 0; i--)
        {
            var slot = slots[i];
            if (slot.hasBuilding && slot.forestTile != null && slot.building != null)
            {
                // Remove cached renderers before destroying
                var renderers = slot.building.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                    _districtRenderers[districtIndex].Remove(r);

                Destroy(slot.building);
                slot.building = null;
                slot.hasBuilding = false;
                slot.forestTile.SetActive(true);
                slots[i] = slot;
                _currentBuildingCount[districtIndex]--;
                _lastHealth[districtIndex] = -1f; // force tint update
                return;
            }
        }
    }

    private GameObject PickPrefab(float gdp)
    {
        float lowChance, midChance;
        if (gdp < 30f)
        {
            lowChance = 0.7f; midChance = 0.3f;
        }
        else if (gdp < 60f)
        {
            lowChance = 0.3f; midChance = 0.5f;
        }
        else
        {
            lowChance = 0.1f; midChance = 0.4f;
        }

        float roll = Random.value;
        GameObject[] tier;
        if (roll < lowChance && prefabConfig.lowTier.Length > 0)
            tier = prefabConfig.lowTier;
        else if (roll < lowChance + midChance && prefabConfig.midTier.Length > 0)
            tier = prefabConfig.midTier;
        else if (prefabConfig.highTier.Length > 0)
            tier = prefabConfig.highTier;
        else if (prefabConfig.midTier.Length > 0)
            tier = prefabConfig.midTier;
        else if (prefabConfig.lowTier.Length > 0)
            tier = prefabConfig.lowTier;
        else
            return null;

        return tier[Random.Range(0, tier.Length)];
    }

    private void ApplyTinting(int districtIndex, DistrictState district)
    {
        float health = (district.happiness + district.sustainability + district.infrastructure) / 3f;
        float roundedHealth = Mathf.Round(health);

        // Skip if health hasn't changed materially
        if (Mathf.Approximately(roundedHealth, _lastHealth[districtIndex])) return;
        _lastHealth[districtIndex] = roundedHealth;

        Color tint = ComputeTintColor(health);
        _propBlock.SetColor(BaseColorId, tint);

        var renderers = _districtRenderers[districtIndex];
        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            if (renderers[i] == null)
                renderers.RemoveAt(i); // clean up destroyed renderers
            else
                renderers[i].SetPropertyBlock(_propBlock);
        }
    }

    private static Color ComputeTintColor(float health)
    {
        if (health >= 70f)
            return Color.Lerp(Color.white, new Color(0.7f, 1f, 0.7f), (health - 70f) / 30f);
        if (health >= 40f)
            return Color.white;
        if (health >= 20f)
            return Color.Lerp(Color.white, new Color(1f, 1f, 0.6f), 1f - (health - 20f) / 20f);
        return Color.Lerp(new Color(1f, 1f, 0.6f), new Color(1f, 0.4f, 0.4f), 1f - health / 20f);
    }
}
