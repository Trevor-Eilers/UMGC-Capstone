// Author: Malcolm Bramble
// Edited by: Trevor Eilers

// Game-level state container. Holds references to networked District objects
// and runtime game state. Not part of the pure simulation layer.
using Simulation;

[System.Serializable]
public struct GameState
{
    public District[] districts;        // networked district GameObjects (for ownership)
    public CityMetrics cityMetrics;     // computed each tick from snapshot
    public int currentTick;             // 0-575
    public int currentMonth;            // 0-47 (currentTick / 12)
    public float gameSpeed;             // 0 (paused), 1, 2, or 3
    public bool isPaused;
    public int numActivePlayers;        // 2-4
}
