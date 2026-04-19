using Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class DetailsPanelController : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root;

        private Label _neighborhoodScore;
        private Label _cityContribScore;
        private Label _finalScore;

        private Label _debtCapTicks;
        private Label _lowHappyTicks;
        private Label _crisisTotal;
        private Label _crisisAvoidance;

        private Label _greenStreak;
        private Label _transitStreak;
        private Label _lifeStreak;
        private Label _devStreak;
        private Label _grantsEligible;

        private Label _totalCitySpend;
        private Label _citySpendShare;

        private Button _closeBtn;

        private Player _localPlayer;
        private bool _eventsHooked;

        private void Start()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc == null)
            {
                Debug.LogError("DetailsPanelController: no UIDocument on GameObject.");
                enabled = false;
                return;
            }

            _root = _doc.rootVisualElement;
            QueryLabels();

            var overlay = _root.Q<VisualElement>("DetailsOverlay");
            if (overlay != null) overlay.style.display = DisplayStyle.Flex;

            _closeBtn = _root.Q<Button>("CloseDetailsBtn");
            if (_closeBtn != null && overlay != null)
                _closeBtn.clicked += () => overlay.style.display = DisplayStyle.None;
        }

        private void QueryLabels()
        {
            _neighborhoodScore = _root.Q<Label>("NeighborhoodScore");
            _cityContribScore = _root.Q<Label>("CityContribScore");
            _finalScore = _root.Q<Label>("FinalScore");

            _debtCapTicks = _root.Q<Label>("DebtCapTicks");
            _lowHappyTicks = _root.Q<Label>("LowHappyTicks");
            _crisisTotal = _root.Q<Label>("CrisisTotal");
            _crisisAvoidance = _root.Q<Label>("CrisisAvoidance");

            _greenStreak = _root.Q<Label>("GreenStreak");
            _transitStreak = _root.Q<Label>("TransitStreak");
            _lifeStreak = _root.Q<Label>("LifeStreak");
            _devStreak = _root.Q<Label>("DevStreak");
            _grantsEligible = _root.Q<Label>("GrantsEligible");

            _totalCitySpend = _root.Q<Label>("TotalCitySpend");
            _citySpendShare = _root.Q<Label>("CitySpendShare");
        }

        private void Update()
        {
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
            var local = _localPlayer?.District;
            if (local == null) return;

            int localIndex = -1;
            int playerCount = GameManager.Instance.PlayerCount;
            for (int i = 0; i < playerCount; i++)
            {
                if (GameManager.Instance.GetPlayer(i).TryGet(out Unity.Netcode.NetworkObject obj) &&
                    obj.GetComponent<Player>() == _localPlayer)
                {
                    localIndex = i;
                    break;
                }
            }
            if (localIndex < 0 || localIndex >= states.Length) return;

            var district = states[localIndex];
            var score = ScoringSystem.ComputeFinalScore(
                district, cityMetrics, states, GameManager.Instance.NumActivePlayers);

            _neighborhoodScore.text = score.neighborhoodScore.ToString("F1");
            _cityContribScore.text = score.cityContribScore.ToString("F1");
            _finalScore.text = score.finalScore.ToString("F1");

            _debtCapTicks.text = district.ticksAtDebtCap.ToString();
            _lowHappyTicks.text = district.ticksBelowHappiness20.ToString();
            int crisisTotal = district.ticksAtDebtCap + district.ticksBelowHappiness20;
            _crisisTotal.text = crisisTotal.ToString();
            float crisisAvoid = Mathf.Max(0f, 100f - crisisTotal * SimulationConstants.K_CRISIS_PENALTY);
            _crisisAvoidance.text = crisisAvoid.ToString("F1");

            _greenStreak.text = $"{district.greenGrantStreak} ticks";
            _transitStreak.text = $"{district.transitGrantStreak} ticks";
            _lifeStreak.text = $"{district.lifeGrantStreak} ticks";
            _devStreak.text = $"{district.devGrantStreak} ticks";
            _grantsEligible.text = district.grantsEligible ? "Yes" : "No";

            _totalCitySpend.text = $"${district.totalCitySpending:F0}";

            float totalAll = 0f;
            for (int i = 0; i < GameManager.Instance.NumActivePlayers; i++)
                totalAll += states[i].totalCitySpending;
            float share = totalAll > 0f
                ? (district.totalCitySpending / totalAll) * 100f
                : 100f / Mathf.Max(1, GameManager.Instance.NumActivePlayers);
            _citySpendShare.text = $"{share:F1}%";
        }

        private void OnDestroy()
        {
            if (_eventsHooked && GameManager.Instance != null)
                GameManager.Instance.OnDistrictStatesUpdated -= HandleTick;
        }
    }
}
