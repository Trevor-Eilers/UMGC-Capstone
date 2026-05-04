// Author: Tyson

using UnityEngine;
using Simulation;

/// <summary>
/// AI opponent component for single-player mode.
/// Attaches to a Player GameObject. Uses AIStrategy to compute
/// policy values each tick. GameManager reads CurrentPolicies
/// instead of PolicySliders for AI-controlled districts.
/// </summary>
public class AIController : MonoBehaviour
{
    [SerializeField]
    public AIStrategy.Profile strategy = AIStrategy.Profile.Balanced;

    [SerializeField]
    private bool reactiveEnabled = true;

    /// <summary>
    /// The current policy values for this AI district.
    /// GameManager reads this each tick.
    /// </summary>
    public PolicyValues CurrentPolicies { get; private set; }

    void Start()
    {
        CurrentPolicies = AIStrategy.GetBasePolicies(strategy);
    }

    /// <summary>
    /// Called each tick with the current district state.
    /// </summary>
    public void OnTick(DistrictState state)
    {
        CurrentPolicies = AIStrategy.ComputePolicies(strategy, state, reactiveEnabled);
    }
}