// Author: Malcolm Bramble

[System.Serializable]
public struct GameState
{
    public DistrictState[] districts;  // one per player (4 max)
    public CityMetrics cityMetrics;
    public int currentTick;            // 0-575
    public int currentMonth;           // 0-47 (currentTick / 12)
    public float gameSpeed;            // 0 (paused), 1, 2, or 3
    public bool isPaused;
    public int numActivePlayers;       // 2-4

    public static GameState NewGame(int numPlayers)
    {
        var state = new GameState
        {
            districts = new DistrictState[4],
            cityMetrics = CityMetrics.Default(),
            currentTick = 0,
            currentMonth = 0,
            gameSpeed = 1f,
            isPaused = false,
            numActivePlayers = numPlayers
        };

        for (int i = 0; i < 4; i++)
        {
            state.districts[i] = DistrictState.Default(i);
        }

        return state;
    }
}
