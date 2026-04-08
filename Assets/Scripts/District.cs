using Simulation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class District : NetworkBehaviour
{
    [SerializeField]
    [ReadOnly]
    public NetworkVariable<DistrictState> state;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
