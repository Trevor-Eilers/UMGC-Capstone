// Author: Malcolm Bramble
// Edited by: Trevor Eilers

namespace Simulation
{
    [System.Serializable]
    public struct GameState
    {
        public DistrictState[] districts;
        public CityMetrics cityMetrics;
        public int currentTick;            // 0-575
        public int currentMonth;           // 0-47 (currentTick / 12)
        public float gameSpeed;            // 0 (paused), 1, 2, or 3
        public bool isPaused;

        public static GameState NewGame(DistrictState[] districts)
        {
            var state = new GameState
            {
                districts = districts,
                cityMetrics = CityMetrics.Default(),
                currentTick = 0,
                currentMonth = 0,
                gameSpeed = 1f,
                isPaused = false,
               
            };

            return state;
        }
    }
}
