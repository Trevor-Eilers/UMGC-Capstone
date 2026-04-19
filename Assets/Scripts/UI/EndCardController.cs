using Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    // Shows a fullscreen modal when the sim reaches tick >= TOTAL_TICKS
    // (i.e. the 48th month has completed). Computes the local player's final
    // score via ScoringSystem.ComputeFinalScore, populates the summary, and
    // provides a "Return to Menu" button that reuses GameManager's normal
    // quit RPC so the session tears down cleanly.
    public class EndCardController : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root;
        private VisualElement _overlay;

        private Label _finalScore;
        private Label _neighborhoodScore;
        private Label _cityScore;
        private Label _districtSummary;
        private Button _returnBtn;

        private Player _localPlayer;
        private bool _eventsHooked;
        private bool _displayed;

        private void Start()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc == null)
            {
                Debug.LogError("EndCardController: no UIDocument on GameObject.");
                enabled = false;
                return;
            }

            // The UIDocument is intentionally disabled in the scene until the
            // game ends, so its empty rootVisualElement doesn't compete with
            // the HUD's UIDocuments on the same shared PanelSettings. We
            // re-enable it in HandleTick once we have scores to show.
        }

        private void Update()
        {
            if (_displayed) return;
            if (_eventsHooked) return;
            if (GameManager.Instance == null) return;

            if (_localPlayer == null)
            {
                foreach (var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
                    if (p.IsOwner) { _localPlayer = p; break; }
                if (_localPlayer == null) return;
            }

            GameManager.Instance.OnDistrictStatesUpdated += HandleTick;
            _eventsHooked = true;
        }

        private void HandleTick(DistrictState[] states, CityMetrics cityMetrics)
        {
            if (_displayed) return;

            // End of the game = month 48 complete = tick 576 reached.
            if (GameManager.GameState.Value.currentTick < SimulationConstants.TOTAL_TICKS)
                return;

            int localIndex = FindLocalPlayerIndex();
            if (localIndex < 0 || localIndex >= states.Length) return;

            var district = states[localIndex];
            var score = ScoringSystem.ComputeFinalScore(
                district, cityMetrics, states, GameManager.Instance.NumActivePlayers);

            // Enable the UIDocument so it starts rendering, THEN populate. Before
            // this moment the document has been disabled so the HUD UIDocuments
            // could render without interference.
            _doc.enabled = true;
            _root = _doc.rootVisualElement;
            _overlay = _root.Q<VisualElement>("EndCardOverlay");
            _finalScore = _root.Q<Label>("EndCardFinalScore");
            _neighborhoodScore = _root.Q<Label>("EndCardNeighborhoodScore");
            _cityScore = _root.Q<Label>("EndCardCityScore");
            _districtSummary = _root.Q<Label>("EndCardDistrictSummary");
            _returnBtn = _root.Q<Button>("EndCardReturnBtn");
            if (_returnBtn != null) _returnBtn.clicked += ReturnToMenu;

            if (_finalScore != null)        _finalScore.text        = score.finalScore.ToString("F1");
            if (_neighborhoodScore != null) _neighborhoodScore.text = score.neighborhoodScore.ToString("F1");
            if (_cityScore != null)         _cityScore.text         = score.cityContribScore.ToString("F1");
            if (_districtSummary != null)   _districtSummary.text   = BuildDistrictSummary(district);

            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
            _displayed = true;

            // Stop listening once we've shown the card.
            if (_eventsHooked && GameManager.Instance != null)
            {
                GameManager.Instance.OnDistrictStatesUpdated -= HandleTick;
                _eventsHooked = false;
            }
        }

        private int FindLocalPlayerIndex()
        {
            if (_localPlayer == null) return -1;
            int playerCount = GameManager.Instance.PlayerCount;
            for (int i = 0; i < playerCount; i++)
            {
                if (GameManager.Instance.GetPlayer(i).TryGet(out Unity.Netcode.NetworkObject obj) &&
                    obj.GetComponent<Player>() == _localPlayer)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string BuildDistrictSummary(DistrictState d)
        {
            int crisisTicks = d.ticksAtDebtCap + d.ticksBelowHappiness20;
            int crisisFree = SimulationConstants.TOTAL_TICKS - crisisTicks;
            return $"GDP {d.gdp:F0}  ·  Happiness {d.happiness:F0}  ·  Pop {d.population:F0}k\n"
                 + $"Infra {d.infrastructure:F0}  ·  Sust {d.sustainability:F0}  ·  Debt {d.debt:F0}\n"
                 + $"Crisis-free ticks: {crisisFree} / {SimulationConstants.TOTAL_TICKS}";
        }

        private void ReturnToMenu()
        {
            if (GameManager.Instance == null) return;
            // Reuse the existing quit path — it tears down the session and loads
            // MenuScene.
            GameManager.Instance.RequestQuitRpc(_localPlayer != null
                ? _localPlayer.NetworkObjectId
                : 0);
        }

        private void OnDestroy()
        {
            if (_eventsHooked && GameManager.Instance != null)
                GameManager.Instance.OnDistrictStatesUpdated -= HandleTick;
        }
    }
}
