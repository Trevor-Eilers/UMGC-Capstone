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
        private Label _grade;
        private Label _neighborhoodScore;
        private Label _cityScore;
        private Label _statGdp;
        private Label _statHappy;
        private Label _statPop;
        private Label _statInfra;
        private Label _statSust;
        private Label _statDebt;
        private Label _crisisFree;
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
            _grade = _root.Q<Label>("EndCardGrade");
            _neighborhoodScore = _root.Q<Label>("EndCardNeighborhoodScore");
            _cityScore = _root.Q<Label>("EndCardCityScore");
            _statGdp = _root.Q<Label>("EndCardStatGDP");
            _statHappy = _root.Q<Label>("EndCardStatHappy");
            _statPop = _root.Q<Label>("EndCardStatPop");
            _statInfra = _root.Q<Label>("EndCardStatInfra");
            _statSust = _root.Q<Label>("EndCardStatSust");
            _statDebt = _root.Q<Label>("EndCardStatDebt");
            _crisisFree = _root.Q<Label>("EndCardCrisisFree");
            _returnBtn = _root.Q<Button>("EndCardReturnBtn");
            if (_returnBtn != null) _returnBtn.clicked += ReturnToMenu;

            if (_finalScore != null)
            {
                _finalScore.text = score.finalScore.ToString("F1");
                _finalScore.style.color = ColorForScore(score.finalScore);
            }
            if (_grade != null) _grade.text = GradeForScore(score.finalScore);
            if (_neighborhoodScore != null) _neighborhoodScore.text = score.neighborhoodScore.ToString("F1");
            if (_cityScore != null)         _cityScore.text         = score.cityContribScore.ToString("F1");

            if (_statGdp != null)   _statGdp.text   = $"{district.gdp:F0}";
            if (_statHappy != null) _statHappy.text = $"{district.happiness:F0}";
            if (_statPop != null)   _statPop.text   = $"{district.population:F0}k";
            if (_statInfra != null) _statInfra.text = $"{district.infrastructure:F0}";
            if (_statSust != null)  _statSust.text  = $"{district.sustainability:F0}";
            if (_statDebt != null)  _statDebt.text  = $"{district.debt:F0}";

            int crisisTicks = district.ticksAtDebtCap + district.ticksBelowHappiness20;
            int crisisFree = SimulationConstants.TOTAL_TICKS - crisisTicks;
            if (_crisisFree != null) _crisisFree.text = $"{crisisFree} / {SimulationConstants.TOTAL_TICKS}";

            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
            _displayed = true;

            // Hide the HUD UIDocuments so they don't bleed through the overlay.
            // They share a PanelSettings with this document, so sortingOrder alone
            // doesn't fully occlude them.
            foreach (var other in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (other == _doc) continue;
                var r = other.rootVisualElement;
                if (r != null) r.style.display = DisplayStyle.None;
            }

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

        private static string GradeForScore(float s)
        {
            if (s >= 85) return "OUTSTANDING";
            if (s >= 70) return "STRONG";
            if (s >= 55) return "SOLID";
            if (s >= 40) return "MIXED";
            return "ROUGH";
        }

        private static Color ColorForScore(float s)
        {
            if (s >= 70) return new Color(0.47f, 0.86f, 0.47f);   // green
            if (s >= 50) return new Color(0.90f, 0.78f, 0.31f);   // amber
            return new Color(0.87f, 0.42f, 0.42f);                // red
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
