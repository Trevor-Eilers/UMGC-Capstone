using System.Collections.Generic;
using System.Linq;
using Simulation;
using UnityEngine;

// Plot-based BuildingGenerator. The scene organises each district as
// NW_DistrictPlots / SW_DistrictPlots / NE_DistrictPlots / SE_DistrictPlots /
// C_DistrictPlots, and each of those contains several "Plots (N)" GameObjects.
// Each Plot has some Forest Tile trees inside and MAY have a hand-placed
// building. We treat each Plot as a single slot: if it already has a building
// from the scene, we leave it alone; otherwise runtime spawning places exactly
// one building at the plot's centre. One plot = one building means overlap is
// impossible by construction.
[ExecuteInEditMode]
public class BuildingGenerator : MonoBehaviour
{
    private GameObject _groundPlane;
    [SerializeField] private BuildingPrefabConfig prefabConfig;

    // How many thousands of residents one building represents. Tune to control
    // city density; at 30, a district with 300k residents fills 10 plots.
    private const float POP_PER_PLOT = 30f;
    private const int BUILDINGS_PER_TICK = 4;

    // Keep spawned prefabs within roughly a plot-sized footprint so they don't
    // poke into roads or neighbouring plots.
    private const float MAX_BUILDING_FOOTPRINT = 14f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private class Plot
    {
        public Transform root;
        public Vector3 center;
        public Bounds plotBounds;           // world-space bounds of the plot (trees + buildings)
        public int district;
        public List<Transform> trees = new();
        public bool hasPrePlacedBuilding;   // scene-authored building in this plot
        public GameObject building;          // runtime-spawned OR pre-placed
        public Bounds buildingBounds;
    }

    private readonly List<Plot>[] _plots = new List<Plot>[4];
    private int[,] _lastTier;                           // [district, category]
    private float[] _lastHealth;                        // per-district tint cache

    private Vector3 _mapCenter;
    private Bounds _mapBounds;

    private MaterialPropertyBlock _propBlock;
    private readonly List<Renderer>[] _districtRenderers = new List<Renderer>[4];

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _lastTier = new int[4, 4];
        _lastHealth = new float[] { -1f, -1f, -1f, -1f };
        // Pre-seed _lastTier with the tiers implied by the starting DistrictState.
        // Previously we initialised to -1, which guaranteed a "tier change"
        // on the very first tick and caused ReskinCategory() to fire across
        // every district/category even when population and metrics hadn't
        // actually moved — visible as a burst of building spawns that the
        // user read as "buildings keep generating" after pause.
        var start = DistrictState.Default();
        for (int d = 0; d < 4; d++)
        {
            _plots[d] = new List<Plot>();
            _districtRenderers[d] = new List<Renderer>();
            _lastTier[d, 0] = SelectTier(BuildingCategory.Residential, start);
            _lastTier[d, 1] = SelectTier(BuildingCategory.Commercial, start);
            _lastTier[d, 2] = SelectTier(BuildingCategory.Industrial, start);
            _lastTier[d, 3] = SelectTier(BuildingCategory.Civic, start);
        }

        ComputeMapCenter();
        CreateGroundPlane();
        // Find every Transform named "Plots (N)" anywhere in the scene. They
        // live under NW_DistrictPlots / SW_DistrictPlots / etc. which are
        // siblings of Map at the scene root, so recursing from `transform`
        // (our own GameObject) would miss them. Only do this when actually
        // playing so [ExecuteInEditMode] doesn't try to spawn in the editor.
        if (Application.isPlaying)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null || !t.name.StartsWith("Plots")) continue;
                var plot = MakePlot(t);
                if (plot != null) _plots[plot.district].Add(plot);
            }
        }

        int totalPlots = 0, totalOccupied = 0;
        for (int d = 0; d < 4; d++)
        {
            int occupied = 0;
            foreach (var p in _plots[d]) if (p.hasPrePlacedBuilding) occupied++;
            totalPlots += _plots[d].Count;
            totalOccupied += occupied;
            Debug.Log($"BuildingGenerator District {d}: {_plots[d].Count} plots, {occupied} pre-placed, {_plots[d].Count - occupied} available");
        }
        Debug.Log($"BuildingGenerator total: {totalPlots} plots, {totalOccupied} pre-placed");
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

    // Walk the scene hierarchy looking for GameObjects whose name starts with
    // "Plots" — those are the per-plot containers Sala set up. For each plot,
    // we compute its centre, collect its trees, and detect any pre-placed
    // building that's parented inside.
    private void CollectPlots(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            string name = child.name;

            if (name.StartsWith("Plots"))
            {
                var plot = MakePlot(child);
                if (plot != null) _plots[plot.district].Add(plot);
                // Don't recurse into plots — their contents are leaf objects.
                continue;
            }

            // Skip already-handled things.
            if (name.StartsWith("Plane") || name.StartsWith("CityGround")) continue;

            CollectPlots(child);
        }
    }

    private Plot MakePlot(Transform root)
    {
        var plot = new Plot { root = root };

        // Use renderer bounds union as the plot's footprint. Gives a reliable
        // centre regardless of how the prefab's pivot sits.
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return null;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        plot.plotBounds = b;
        plot.center = new Vector3(b.center.x, 0.05f, b.center.z);
        plot.district = GetDistrict(plot.center);

        // Classify each direct child.
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            string cn = c.name;

            if (IsBuilding(cn))
            {
                // Pre-placed building: this plot is already occupied. Don't
                // touch it at runtime.
                plot.hasPrePlacedBuilding = true;
                plot.building = c.gameObject;
                var cr = c.GetComponentsInChildren<Renderer>(true);
                if (cr.Length > 0)
                {
                    Bounds cb = cr[0].bounds;
                    for (int j = 1; j < cr.Length; j++) cb.Encapsulate(cr[j].bounds);
                    plot.buildingBounds = cb;
                }
                else
                {
                    plot.buildingBounds = new Bounds(c.position, Vector3.one * 5f);
                }
            }
            else if (cn.StartsWith("Forest Tile"))
            {
                plot.trees.Add(c);
            }
            // Street lights, side pieces, etc. are ignored.
        }

        return plot;
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

    // --- public sim hook --------------------------------------------------

    public void UpdateDistrict(int districtIndex, DistrictState state)
    {
        if (districtIndex < 0 || districtIndex >= 4) return;
        if (prefabConfig == null) return;

        var plots = _plots[districtIndex];
        int plotCount = plots.Count;
        if (plotCount == 0) return;

        // How many plots should be built out total? Cap at the number of plots
        // available and count pre-placed ones toward the quota.
        int targetOccupied = Mathf.Clamp(
            Mathf.RoundToInt(state.population / POP_PER_PLOT),
            0, plotCount);

        int currentOccupied = 0;
        int currentRuntime = 0;
        foreach (var p in plots)
        {
            if (p.building != null) currentOccupied++;
            if (p.building != null && !p.hasPrePlacedBuilding) currentRuntime++;
        }

        if (currentOccupied < targetOccupied)
        {
            int toSpawn = Mathf.Min(targetOccupied - currentOccupied, BUILDINGS_PER_TICK);
            for (int s = 0; s < toSpawn; s++)
                if (!FillNextEmptyPlot(districtIndex, state)) break;
        }
        else if (currentOccupied > targetOccupied && currentRuntime > 0)
        {
            int toRemove = Mathf.Min(currentOccupied - targetOccupied, BUILDINGS_PER_TICK);
            for (int s = 0; s < toRemove; s++)
                if (!RemoveLastRuntimeBuilding(districtIndex)) break;
        }

        // On tier change, re-skin runtime-spawned buildings in that category.
        for (int c = 0; c < 4; c++)
        {
            var cat = (BuildingCategory)c;
            int tier = SelectTier(cat, state);
            if (tier != _lastTier[districtIndex, c])
            {
                _lastTier[districtIndex, c] = tier;
                ReskinCategory(districtIndex, cat, tier);
            }
        }

        ApplyTinting(districtIndex, state);
    }

    // --- placement --------------------------------------------------------

    private bool FillNextEmptyPlot(int districtIndex, DistrictState state)
    {
        var plots = _plots[districtIndex];
        for (int i = 0; i < plots.Count; i++)
        {
            var p = plots[i];
            if (p.building != null) continue; // already built

            var cat = (BuildingCategory)(i & 3);
            int tier = SelectTier(cat, state);
            var pool = prefabConfig.GetTier(cat, tier);
            if (pool == null || pool.Length == 0) continue;

            var prefab = pool[i % pool.Length];
            if (prefab == null) continue;

            // Hide the trees in this plot so the building doesn't clip them.
            foreach (var t in p.trees)
                if (t != null) t.gameObject.SetActive(false);

            float yaw = 90f * (i % 4);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var instance = Instantiate(prefab, p.center, rot, transform);
            NormalizeScaleInPlace(instance);

            p.building = instance;
            p.buildingBounds = RecomputeBounds(instance);
            _districtRenderers[districtIndex].AddRange(instance.GetComponentsInChildren<Renderer>());
            _lastHealth[districtIndex] = -1f;
            return true;
        }
        return false;
    }

    private bool RemoveLastRuntimeBuilding(int districtIndex)
    {
        var plots = _plots[districtIndex];
        for (int i = plots.Count - 1; i >= 0; i--)
        {
            var p = plots[i];
            if (p.building == null || p.hasPrePlacedBuilding) continue;

            var rends = p.building.GetComponentsInChildren<Renderer>();
            foreach (var r in rends) _districtRenderers[districtIndex].Remove(r);
            Destroy(p.building);
            p.building = null;
            p.buildingBounds = default;
            // Bring trees back so the plot doesn't look bare.
            foreach (var t in p.trees)
                if (t != null) t.gameObject.SetActive(true);
            _lastHealth[districtIndex] = -1f;
            return true;
        }
        return false;
    }

    // Respawn runtime-spawned buildings in a category at the new tier. Pre-
    // placed buildings are left alone so the scene's hand authored look
    // survives tier changes.
    private void ReskinCategory(int districtIndex, BuildingCategory cat, int tier)
    {
        var pool = prefabConfig.GetTier(cat, tier);
        if (pool == null || pool.Length == 0) return;

        var plots = _plots[districtIndex];
        for (int i = 0; i < plots.Count; i++)
        {
            if ((i & 3) != (int)cat) continue;
            var p = plots[i];
            if (p.building == null) continue;
            if (p.hasPrePlacedBuilding) continue;

            var rends = p.building.GetComponentsInChildren<Renderer>();
            foreach (var r in rends) _districtRenderers[districtIndex].Remove(r);
            Destroy(p.building);
            p.building = null;
            p.buildingBounds = default;

            var prefab = pool[i % pool.Length];
            if (prefab == null) continue;
            float yaw = 90f * (i % 4);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var instance = Instantiate(prefab, p.center, rot, transform);
            NormalizeScaleInPlace(instance);

            p.building = instance;
            p.buildingBounds = RecomputeBounds(instance);
            _districtRenderers[districtIndex].AddRange(instance.GetComponentsInChildren<Renderer>());
        }
        _lastHealth[districtIndex] = -1f;
    }

    // Measure an already-spawned instance and shrink it if too big for a plot.
    // Doing this on the live instance (instead of from a cached prefab bounds)
    // means we never have to Instantiate prefabs just to measure them, which
    // was what was triggering Unity's "Persistent allocates N individual
    // allocations" leak warning.
    private static void NormalizeScaleInPlace(GameObject instance)
    {
        var rends = instance.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        float largest = Mathf.Max(b.size.x, b.size.z);
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

    // --- tier / category --------------------------------------------------

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

    // --- tinting ----------------------------------------------------------

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
            if (renderers[i] == null) renderers.RemoveAt(i);
            else renderers[i].SetPropertyBlock(_propBlock);
        }
    }

    private static Color ComputeTintColor(float health)
    {
        if (health >= 70f)
            return Color.Lerp(Color.white, new Color(0.7f, 1f, 0.7f), (health - 70f) / 30f);
        if (health >= 40f) return Color.white;
        if (health >= 20f)
            return Color.Lerp(Color.white, new Color(1f, 1f, 0.6f), 1f - (health - 20f) / 20f);
        return Color.Lerp(new Color(1f, 1f, 0.6f), new Color(1f, 0.4f, 0.4f), 1f - health / 20f);
    }

    // --- helpers ----------------------------------------------------------

    private static bool IsBuilding(string name)
    {
        return name.StartsWith("SmallHouse") || name.StartsWith("OldHouse") ||
               name.StartsWith("SuburbHouse") || name.StartsWith("House") ||
               name.StartsWith("Factory") || name.StartsWith("Hotel") ||
               name.StartsWith("Bank") || name.StartsWith("Church") ||
               name.StartsWith("CityHall") || name.StartsWith("Hospital") ||
               name.StartsWith("Police") || name.StartsWith("FireStation") ||
               name.StartsWith("Cafe") || name.StartsWith("Coffee") ||
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
}