using System.Collections.Generic;
using UnityEngine;

public class DistrictCivilianSpawner : MonoBehaviour
{
    [Header("Population")]
    public float population = 100f;
    public int peoplePerOrb = 50;
    public int maxCivilians = 25;

    [Header("Setup")]
    public GameObject civilianPrefab;
    public List<RoadNode> districtNodes = new List<RoadNode>();

    [Header("Auto Refresh")]
    public float refreshRate = 3f;

    private float timer;
    private int lastCivilianCount = -1;

    private List<GameObject> civilians = new List<GameObject>();

    void Start()
    {
        RefreshCivilians();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= refreshRate)
        {
            timer = 0f;
            RefreshIfNeeded();
        }
    }

    void RefreshIfNeeded()
    {
        int wantedCount = GetCivilianCount();

        if (wantedCount != lastCivilianCount)
        {
            RefreshCivilians();
        }
    }

    public void SetPopulation(float newPopulation)
    {
        population = newPopulation;
        RefreshIfNeeded();
    }

    public void RefreshCivilians()
    {
        ClearCivilians();

        int civilianCount = GetCivilianCount();
        lastCivilianCount = civilianCount;

        for (int i = 0; i < civilianCount; i++)
        {
            SpawnCivilian();
        }
    }

    int GetCivilianCount()
    {
        if (peoplePerOrb <= 0)
        {
            peoplePerOrb = 50;
        }

        int count = Mathf.FloorToInt(population / peoplePerOrb);

        return Mathf.Clamp(count, 1, maxCivilians);
    }

    void SpawnCivilian()
    {
        if (civilianPrefab == null || districtNodes.Count == 0) return;

        RoadNode startNode = districtNodes[Random.Range(0, districtNodes.Count)];

        GameObject civ = Instantiate(civilianPrefab);

        CivilianWalker walker = civ.GetComponent<CivilianWalker>();

        if (walker != null)
        {
            walker.SetStartNode(startNode);
        }

        civilians.Add(civ);
    }

    void ClearCivilians()
    {
        for (int i = 0; i < civilians.Count; i++)
        {
            if (civilians[i] != null)
            {
                Destroy(civilians[i]);
            }
        }

        civilians.Clear();
    }
}