using System.Collections.Generic;
using UnityEngine;

public class DistrictPlot : MonoBehaviour
{
    private GameObject[] _plots;
    public HashSet<int> UnoccupiedIndices { get; private set; }
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
        if (UnoccupiedIndices.Contains(i)) 
            throw new System.Exception("Cannot add to already occupied plot");
        
        _buildings[i] = Instantiate(prefab, _plots[i].transform.position, _plots[i].transform.rotation);
        UnoccupiedIndices.Remove(i);
    }
    
    public void RemoveAt(int i)
    {
        if (!UnoccupiedIndices.Contains(i)) return;
        
        UnoccupiedIndices.Add(i);
        Destroy(_buildings[i]);
        _buildings[i] = null;
    }
}
