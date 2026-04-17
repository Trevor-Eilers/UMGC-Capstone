using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DistrictSquare : MonoBehaviour
{
    private GameObject[] _plots;
    public HashSet<int> UnoccupiedIndices { get; private set; } = new();
    private GameObject[] _buildings;

    void Start()
    {
        _plots = new GameObject[transform.childCount];
        _buildings = new GameObject[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            _plots[i] = transform.GetChild(i).gameObject;
            UnoccupiedIndices.Add(i);
        }

    }

    public void Add(int i, GameObject prefab)
    {
        if (!UnoccupiedIndices.Contains(i))
            throw new System.Exception("Cannot add to already occupied plot");

        _buildings[i] = Instantiate(prefab, _plots[i].transform.position, _plots[i].transform.rotation);
        UnoccupiedIndices.Remove(i);
    }

    public bool TryAddRandom(GameObject prefab)
    {
        if (UnoccupiedIndices.Count == 0) return false;

        int position = Random.Range(0, UnoccupiedIndices.Count);
        int plotIndex = UnoccupiedIndices.ElementAt(position);
        Add(plotIndex, prefab);
        return true;
    }

    public void RemoveAt(int i)
    {
        if (!UnoccupiedIndices.Add(i)) return;

        Destroy(_buildings[i]);
        _buildings[i] = null;
    }
}
