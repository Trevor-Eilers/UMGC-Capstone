using System.Collections.Generic;
using Core;
using Simulation;
using UnityEngine;

public class DistrictCivilianSpawner : MonoBehaviour
{
    [Header("Population")]
    public int peoplePerOrb = 10;
    public int maxCivilians = 25;

    [Header("Setup")]
    public GameObject civilianPrefab;
    public List<RoadNode> districtNodes = new List<RoadNode>();

    private readonly List<GameObject> civilians = new List<GameObject>();
    private readonly List<Renderer> _renderers = new List<Renderer>();

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _propBlock;
    private Color _currentTint = Color.white;
    private float _lastHappiness = float.NaN;

    private District _district;

    public District District
    {
        get => _district;
        set
        {
            if (_district == value) return;
            Unsubscribe();
            _district = value;
            Subscribe();
        }
    }

    private void Subscribe()
    {
        if (_district == null) return;
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
        _district.state.OnValueChanged += OnStateChanged;
        UpdateDistrict(_district.state.Value);
        ApplyTinting(_district.state.Value);
    }

    private void Unsubscribe()
    {
        if (_district == null) return;
        _district.state.OnValueChanged -= OnStateChanged;
    }

    private void OnStateChanged(DistrictState prev, DistrictState next)
    {
        UpdateDistrict(next);
        ApplyTinting(next);
    }

    private void OnDestroy() => Unsubscribe();

    public void UpdateDistrict(DistrictState state)
    {
        if (peoplePerOrb <= 0) peoplePerOrb = 10000;

        int target = Mathf.Clamp(Mathf.FloorToInt(state.population / peoplePerOrb), 0, maxCivilians);
        int current = civilians.Count;

        if (current < target)
        {
            for (int i = 0; i < target - current; i++) SpawnCivilian();
        }
        else if (current > target)
        {
            for (int i = 0; i < current - target; i++) RemoveCivilian();
        }
    }

    private void SpawnCivilian()
    {
        if (civilianPrefab == null || districtNodes.Count == 0) return;

        RoadNode startNode = districtNodes[Random.Range(0, districtNodes.Count)];
        GameObject civ = Instantiate(civilianPrefab);

        var walker = civ.GetComponent<CivilianWalker>();
        if (walker != null) walker.SetStartNode(startNode);

        civilians.Add(civ);

        var rs = civ.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            _renderers.AddRange(rs);
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _propBlock.SetColor(BaseColorId, _currentTint);
            foreach (var r in rs)
                if (r != null) r.SetPropertyBlock(_propBlock);
        }
    }

    private void ApplyTinting(DistrictState state)
    {
        float happiness = state.happiness;
        float rounded = Mathf.Round(happiness);
        if (Mathf.Approximately(rounded, _lastHappiness)) return;
        _lastHappiness = rounded;

        _currentTint = ComputeTintColor(happiness);
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
        _propBlock.SetColor(BaseColorId, _currentTint);

        for (int i = _renderers.Count - 1; i >= 0; i--)
        {
            if (_renderers[i] == null) _renderers.RemoveAt(i);
            else _renderers[i].SetPropertyBlock(_propBlock);
        }
    }

    private static Color ComputeTintColor(float happiness)
    {
        float t = Mathf.Clamp01(happiness / 100f);
        if (t < 0.5f)
            return Color.Lerp(new Color(1f, 0.2f, 0.2f), Color.white, t / 0.5f);
        return Color.Lerp(Color.white, new Color(0.2f, 1f, 0.2f), (t - 0.5f) / 0.5f);
    }

    private void RemoveCivilian()
    {
        int last = civilians.Count - 1;
        if (last < 0) return;

        var go = civilians[last];
        civilians.RemoveAt(last);
        if (go != null) Destroy(go);
    }
}
