using System.Collections.Generic;
using UnityEngine;

public class DistrictSquare : MonoBehaviour
{
    private GameObject[] _plots;
    public HashSet<int> UnoccupiedIndices { get; private set; } = new();
    private GameObject[] _buildings;

    public int PlotCount => _plots?.Length ?? 0;
    public int OccupiedCount => PlotCount - UnoccupiedIndices.Count;

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

    public GameObject Add(int i, GameObject prefab)
    {
        if (!UnoccupiedIndices.Contains(i))
            throw new System.Exception("Cannot add to already occupied plot");

        _buildings[i] = Instantiate(prefab, _plots[i].transform.position, _plots[i].transform.rotation);
        UnoccupiedIndices.Remove(i);
        return _buildings[i];
    }

    public void RemoveAt(int i)
    {
        if (!UnoccupiedIndices.Add(i)) return;

        Destroy(_buildings[i]);
        _buildings[i] = null;
    }
}
