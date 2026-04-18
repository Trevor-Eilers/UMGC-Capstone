using System.Collections.Generic;
using Simulation;
using UnityEngine;

[ExecuteInEditMode]
public class BuildingGenerator : MonoBehaviour
{
    private GameObject _groundPlane;
    [SerializeField] private BuildingPrefabConfig prefabConfig;

    private const float POP_PER_BUILDING = 12f;
    private const int MAX_BUILDINGS_PER_DISTRICT = 50;
    private const int BUILDINGS_PER_TICK = 10;

    // Placement tuning — all in world units. Reference: a single Forest Tile is
    // roughly a 10-unit square in the LowPolyMegapolis map.
    private const float SLOT_MIN_SPACING = 8f;       // Step 3: dedup Forest Tiles
    private const float ROAD_BUFFER = 1.0f;          // Step 2: extra padding around roads
    private const float MAX_BUILDING_FOOTPRINT = 12f; // Step 6: scale clamp

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly float[] ValidRotations = { 0f, 90f, 180f, 270f };

    private struct BuildingSlot
    {
        public Vector3 position;
        public GameObject forestTile;
        public GameObject building;
        public bool hasBuilding;
        public Bounds occupiedBounds; // world-space AABB of the placed building, if any
    }

    private List<BuildingSlot>[] _districtSlots;
    private int[] _currentBuildingCount;
    private Vector3 _mapCenter;
    private Bounds _mapBounds;
    private MaterialPropertyBlock _propBlock;
    private List<Renderer>[] _districtRenderers;
    private float[] _lastHealth;
    private int[,] _lastTier;

    // Step 2: cached prefab AABBs in LOCAL space (measured once at Awake).
    private readonly Dictionary<GameObject, Bounds> _prefabLocalBounds = new();

    private readonly List<Bounds> _roadBounds = new();
    private Bounds _cityGridBounds;

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _districtSlots = new List<BuildingSlot>[4];
        _currentBuildingCount = new int[4];

        _districtRenderers = new List<Renderer>[4];
        _lastHealth = new float[] { -1f, -1f, -1f, -1f };
        _lastTier = new int[4, 4];
        for (int d = 0; d < 4; d++)
            for (int c = 0; c < 4; c++)
                _lastTier[d, c] = -1;
        for (int i = 0; i < 4; i++)
        {
            _districtSlots[i] = new List<BuildingSlot>();
            _districtRenderers[i] = new List<Renderer>();
        }

        ComputeMapCenter();
        CreateGroundPlane();
        CollectSlots();
        SortSlotsByDistanceFromCenter();
        CachePrefabBounds();

        for (int i = 0; i < 4; i++)
            Debug.Log($"BuildingGenerator District {i}: {_districtSlots[i].Count} slots, {_currentBuildingCount[i]} buildings");
        Debug.Log($"BuildingGenerator: cached bounds for {_prefabLocalBounds.Count} prefabs, collected {_roadBounds.Count} road bounds, grid={_cityGridBounds}");
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
        if (!Application.isPlaying)
        {
            ComputeMapCenter();
            CreateGroundPlane();
        }
    }

    void OnDisable()
    {
        if (!Application.isPlaying)
        {
            var existing = transform.Find("CityGround")?.gameObject;
            if (existing != null) DestroyImmediate(existing);
        }
    }

    private void CollectSlots()
    {
        CollectExistingBuildings(transform);
        ComputeCityGridBounds();
        CollectRoadBounds(transform);
        CollectForestTiles(transform);
    }

    private void ComputeCityGridBounds()
    {
        bool first = true;
        for (int d = 0; d < 4; d++)
        {
            foreach (var slot in _districtSlots[d])
            {
                if (!slot.hasBuilding) continue;
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
        _cityGridBounds.Expand(20f);
    }

    // Step 1: case-insensitive road-name match so "road stretch", "MobileRoad",
    // "Intersection", "Sidewalk", "Crosswalk" all feed into _roadBounds.
    private static bool IsRoadName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        string n = name.ToLowerInvariant();
        return n.Contains("road") ||
               n.Contains("mobileroad") ||
               n.Contains("intersection") ||
               n.Contains("sidewalk") ||
               n.Contains("crosswalk") ||
               n.Contains("street");
    }

    private void CollectRoadBounds(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            string name = child.gameObject.name;

            if (IsRoadName(name))
            {
                var renderers = child.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                    _roadBounds.Add(r.bounds);
            }

            if (!IsBuilding(name) && !name.StartsWith("Forest Tile")
                && !name.StartsWith("Plane") && !name.StartsWith("CityGround"))
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
                if (!_cityGridBounds.Contains(child.position)) continue;
                if (IsTooCloseToRoad(child.position)) continue;

                int district = GetDistrict(child.position);

                // Step 3: skip tiles that are too close to a tile we already accepted
                // for this district. Prevents dense clumps from producing overlapping
                // buildings.
                if (IsTooCloseToExistingSlot(child.position, district)) continue;

                _districtSlots[district].Add(new BuildingSlot
                {
                    position = child.position,
                    forestTile = child.gameObject,
                    building = null,
                    hasBuilding = false,
                    occupiedBounds = default
                });
            }
            else if (!IsBuilding(name) && !name.StartsWith("Plane") && !name.StartsWith("CityGround"))
            {
                CollectForestTiles(child);
            }
        }
    }

    private bool IsTooCloseToExistingSlot(Vector3 pos, int district)
    {
        float sqrThreshold = SLOT_MIN_SPACING * SLOT_MIN_SPACING;
        var slots = _districtSlots[district];
        for (int i = 0; i < slots.Count; i++)
        {
            if ((slots[i].position - pos).sqrMagnitude < sqrThreshold)
                return true;
        }
        return false;
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
                var rends = child.GetComponentsInChildren<Renderer>(true);
                Bounds b = default;
                bool hasBounds = false;
                foreach (var r in rends)
                {
                    if (!hasBounds) { b = r.bounds; hasBounds = true; }
                    else b.Encapsulate(r.bounds);
                }

                _districtSlots[district].Add(new BuildingSlot
                {
                    position = child.position,
                    forestTile = null,
                    building = child.gameObject,
                    hasBuilding = true,
                    occupiedBounds = hasBounds ? b : new Bounds(child.position, Vector3.one * 5f)
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

    // Step 2: measure each prefab once so we can project a world AABB at any
    // target position/rotation without instantiating first. Runs in Awake.
    private void CachePrefabBounds()
    {
        if (prefabConfig == null) return;

        foreach (BuildingCategory cat in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            for (int tier = 0; tier < 3; tier++)
            {
                var pool = prefabConfig.GetTier(cat, tier);
                if (pool == null) continue;
                foreach (var prefab in pool)
                {
                    if (prefab == null || _prefabLocalBounds.ContainsKey(prefab)) continue;
                    _prefabLocalBounds[prefab] = ComputePrefabLocalBounds(prefab);
                }
            }
        }
    }

    private static Bounds ComputePrefabLocalBounds(GameObject prefab)
    {
        // Instantiate at origin with identity rotation so `renderer.bounds` is
        // effectively the local/world AABB. Destroy immediately.
        var temp = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        temp.hideFlags = HideFlags.HideAndDontSave;

        var rends = temp.GetComponentsInChildren<Renderer>(true);
        Bounds b;
        if (rends.Length == 0)
        {
            b = new Bounds(Vector3.zero, Vector3.one * 5f);
        }
        else
        {
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
        }

        DestroyImmediate(temp);
        return b;
    }

    // Project a cached local-space AABB onto world space at a target position
    // and 0/90/180/270 rotation. Since rotation is axis-aligned multiples of 90,
    // we can just swap X and Z extents as needed.
    private static Bounds ProjectAABB(Bounds local, Vector3 worldPos, Quaternion rot)
    {
        Vector3 size = local.size;
        Vector3 center = local.center;

        float yaw = rot.eulerAngles.y;
        bool swapped = Mathf.Approximately(yaw % 180f, 90f);
        Vector3 worldSize = swapped
            ? new Vector3(size.z, size.y, size.x)
            : size;

        // After rotation around Y, the local center also rotates. For 90° multiples
        // this is a simple swap/negate.
        Vector3 rotatedCenter = rot * center;
        return new Bounds(worldPos + rotatedCenter, worldSize);
    }

    private bool BoundsIntersectsRoad(Bounds aabb)
    {
        var expanded = aabb;
        expanded.Expand(ROAD_BUFFER * 2f); // buffer on all sides
        for (int i = 0; i < _roadBounds.Count; i++)
        {
            if (expanded.Intersects(_roadBounds[i]))
                return true;
        }
        return false;
    }

    private bool BoundsIntersectsPlaced(Bounds aabb, int district, int skipSlotIndex)
    {
        var slots = _districtSlots[district];
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == skipSlotIndex) continue;
            var s = slots[i];
            if (!s.hasBuilding) continue;
            if (s.occupiedBounds.size.sqrMagnitude < 0.01f) continue; // unknown bounds, skip
            if (aabb.Intersects(s.occupiedBounds))
                return true;
        }
        return false;
    }

    // Step 5: rotate +Z to face the nearest road edge so doorways point toward
    // streets.
    private float RotationFacingNearestRoad(Vector3 pos)
    {
        if (_roadBounds.Count == 0) return 0f;

        Bounds nearest = _roadBounds[0];
        float bestSqr = nearest.SqrDistance(pos);
        for (int i = 1; i < _roadBounds.Count; i++)
        {
            float s = _roadBounds[i].SqrDistance(pos);
            if (s < bestSqr) { bestSqr = s; nearest = _roadBounds[i]; }
        }

        Vector3 dir = nearest.ClosestPoint(pos) - pos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return 0f;

        // Snap to cardinal: +X, -X, +Z, -Z.
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            return dir.x > 0f ? 90f : 270f;
        return dir.z > 0f ? 0f : 180f;
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
                if (!SpawnBuilding(districtIndex, district)) break;
        }
        else if (targetBuildings < current)
        {
            int toRemove = Mathf.Min(current - targetBuildings, BUILDINGS_PER_TICK);
            for (int s = 0; s < toRemove; s++)
                RemoveBuilding(districtIndex);
        }

        for (int c = 0; c < 4; c++)
        {
            var cat = (BuildingCategory)c;
            int tier = SelectTier(cat, district);
            if (tier != _lastTier[districtIndex, c])
            {
                _lastTier[districtIndex, c] = tier;
                ReskinCategory(districtIndex, cat, tier);
            }
        }

        ApplyTinting(districtIndex, district);
    }

    private bool SpawnBuilding(int districtIndex, DistrictState district)
    {
        var slots = _districtSlots[districtIndex];

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.hasBuilding) continue;

            var cat = CategoryForSlot(i);
            int tier = SelectTier(cat, district);
            var pool = prefabConfig.GetTier(cat, tier);
            if (pool == null || pool.Length == 0) continue;

            if (!TryPlacePrefab(slot.position, pool, i, districtIndex,
                out GameObject placed, out Bounds placedBounds, out float placedRot))
                continue;

            if (slot.forestTile != null) slot.forestTile.SetActive(false);

            slot.building = placed;
            slot.hasBuilding = true;
            slot.occupiedBounds = placedBounds;
            slots[i] = slot;
            _currentBuildingCount[districtIndex]++;
            _districtRenderers[districtIndex].AddRange(placed.GetComponentsInChildren<Renderer>());
            _lastHealth[districtIndex] = -1f;
            return true;
        }
        return false;
    }

    private bool TryPlacePrefab(Vector3 pos, GameObject[] pool, int slotIndex, int districtIndex,
        out GameObject placed, out Bounds placedBounds, out float rotationDeg)
    {
        placed = null;
        placedBounds = default;
        rotationDeg = 0f;
        if (pool == null || pool.Length == 0) return false;

        float preferredRot = RotationFacingNearestRoad(pos);

        // Try up to 4 prefabs × 4 rotations, starting from slotIndex-derived pick.
        int prefabTries = Mathf.Min(4, pool.Length);
        for (int p = 0; p < prefabTries; p++)
        {
            var prefab = pool[(slotIndex + p) % pool.Length];
            if (prefab == null) continue;
            if (!_prefabLocalBounds.TryGetValue(prefab, out var localB))
                localB = ComputePrefabLocalBounds(prefab);

            for (int r = 0; r < 4; r++)
            {
                float yaw = Mathf.Repeat(preferredRot + r * 90f, 360f);
                var rot = Quaternion.Euler(0f, yaw, 0f);
                var worldAABB = ProjectAABB(localB, pos, rot);

                if (BoundsIntersectsRoad(worldAABB)) continue;
                if (BoundsIntersectsPlaced(worldAABB, districtIndex, slotIndex)) continue;

                // Fits — instantiate and optionally scale down if oversized.
                var instance = Instantiate(prefab, pos, rot, transform);
                NormalizeScale(instance, localB);
                placed = instance;
                placedBounds = RecomputeBounds(instance);
                rotationDeg = yaw;
                return true;
            }
        }
        return false;
    }

    // Step 6: cap the largest XZ dimension of spawned prefabs. Only shrinks —
    // never upscales — so small prefabs keep their natural size.
    private static void NormalizeScale(GameObject instance, Bounds localBounds)
    {
        float largest = Mathf.Max(localBounds.size.x, localBounds.size.z);
        if (largest <= MAX_BUILDING_FOOTPRINT) return;
        float s = MAX_BUILDING_FOOTPRINT / largest;
        instance.transform.localScale = instance.transform.localScale * s;
    }

    private static Bounds RecomputeBounds(GameObject instance)
    {
        var rends = instance.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(instance.transform.position, Vector3.one * 5f);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }

    // Step 4: ReskinCategory only touches slots that originated from Forest Tiles.
    // Pre-placed scene buildings (forestTile == null) are left alone so the
    // level designer's arrangements stay intact.
    private void ReskinCategory(int districtIndex, BuildingCategory cat, int tier)
    {
        var pool = prefabConfig.GetTier(cat, tier);
        if (pool == null || pool.Length == 0) return;

        var slots = _districtSlots[districtIndex];
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (!slot.hasBuilding) continue;
            if (slot.forestTile == null) continue; // protect pre-placed
            if (CategoryForSlot(i) != cat) continue;
            if (slot.building == null) continue;

            var oldRenderers = slot.building.GetComponentsInChildren<Renderer>();
            foreach (var r in oldRenderers) _districtRenderers[districtIndex].Remove(r);
            Destroy(slot.building);
            slot.building = null;
            slot.hasBuilding = false;
            slot.occupiedBounds = default;
            _currentBuildingCount[districtIndex]--;
            slots[i] = slot;

            if (!TryPlacePrefab(slot.position, pool, i, districtIndex,
                out GameObject placed, out Bounds placedBounds, out _))
            {
                // Couldn't fit the new tier — leave the slot empty and reactivate
                // the forest tile as a placeholder.
                if (slot.forestTile != null) slot.forestTile.SetActive(true);
                continue;
            }

            slot.building = placed;
            slot.hasBuilding = true;
            slot.occupiedBounds = placedBounds;
            _currentBuildingCount[districtIndex]++;
            slots[i] = slot;
            _districtRenderers[districtIndex].AddRange(placed.GetComponentsInChildren<Renderer>());
        }
        _lastHealth[districtIndex] = -1f;
    }

    private void RemoveBuilding(int districtIndex)
    {
        var slots = _districtSlots[districtIndex];

        for (int i = slots.Count - 1; i >= 0; i--)
        {
            var slot = slots[i];
            if (slot.hasBuilding && slot.forestTile != null && slot.building != null)
            {
                var renderers = slot.building.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                    _districtRenderers[districtIndex].Remove(r);

                Destroy(slot.building);
                slot.building = null;
                slot.hasBuilding = false;
                slot.occupiedBounds = default;
                slot.forestTile.SetActive(true);
                slots[i] = slot;
                _currentBuildingCount[districtIndex]--;
                _lastHealth[districtIndex] = -1f;
                return;
            }
        }
    }

    private static BuildingCategory CategoryForSlot(int slotIndex)
        => (BuildingCategory)(slotIndex & 3);

    private static int SelectTier(BuildingCategory cat, DistrictState d) => cat switch
    {
        BuildingCategory.Residential => TierFromMetric(d.population, 100f, 300f),
        BuildingCategory.Commercial => TierFromMetric(d.gdp, 30f, 60f),
        BuildingCategory.Industrial => TierFromMetric(d.infrastructure, 30f, 60f),
        BuildingCategory.Civic => TierFromMetric(d.happiness, 30f, 60f),
        _ => 1
    };

    private static int TierFromMetric(float value, float lowCut, float highCut)
    {
        if (value < lowCut) return 0;
        if (value < highCut) return 1;
        return 2;
    }

    private void ApplyTinting(int districtIndex, DistrictState district)
    {
        float health = (district.happiness + district.sustainability + district.infrastructure) / 3f;
        float roundedHealth = Mathf.Round(health);

        if (Mathf.Approximately(roundedHealth, _lastHealth[districtIndex])) return;
        _lastHealth[districtIndex] = roundedHealth;

        Color tint = ComputeTintColor(health);
        _propBlock.SetColor(BaseColorId, tint);

        var renderers = _districtRenderers[districtIndex];
        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            if (renderers[i] == null)
                renderers.RemoveAt(i);
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
