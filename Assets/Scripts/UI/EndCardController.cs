using System.Collections;
using Core;
using Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    // Shows a fullscreen end-of-game summary. Call Show(district, score) when the
    // sim ends; the overlay is hidden via display:none until then. Hide() dismisses it.
    public class EndCardController : UIPresenterBase
    {
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

        private void Start()
        {
            StartCoroutine(InitializeHidden());
        }

        private IEnumerator InitializeHidden()
        {
            yield return WaitForRoot();

            _overlay          = root.Q<VisualElement>("EndCardOverlay");
            _finalScore       = root.Q<Label>("EndCardFinalScore");
            _grade            = root.Q<Label>("EndCardGrade");
            _neighborhoodScore = root.Q<Label>("EndCardNeighborhoodScore");
            _cityScore        = root.Q<Label>("EndCardCityScore");
            _statGdp          = root.Q<Label>("EndCardStatGDP");
            _statHappy        = root.Q<Label>("EndCardStatHappy");
            _statPop          = root.Q<Label>("EndCardStatPop");
            _statInfra        = root.Q<Label>("EndCardStatInfra");
            _statSust         = root.Q<Label>("EndCardStatSust");
            _statDebt         = root.Q<Label>("EndCardStatDebt");
            _crisisFree       = root.Q<Label>("EndCardCrisisFree");
            _returnBtn        = root.Q<Button>("EndCardReturnBtn");

            if (_returnBtn != null) _returnBtn.clicked += ReturnToMenu;
            if (_overlay != null)   _overlay.style.display = DisplayStyle.None;
        }

        public void Show(DistrictState district, FinalScore score)
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;

            if (_finalScore != null)
            {
                _finalScore.text        = score.finalScore.ToString("F1");
                _finalScore.style.color = ColorForScore(score.finalScore);
            }
            if (_grade             != null) _grade.text             = GradeForScore(score.finalScore);
            if (_neighborhoodScore != null) _neighborhoodScore.text = score.neighborhoodScore.ToString("F1");
            if (_cityScore         != null) _cityScore.text         = score.cityContribScore.ToString("F1");

            if (_statGdp   != null) _statGdp.text   = $"{district.gdp:F0}";
            if (_statHappy != null) _statHappy.text = $"{district.happiness:F0}";
            if (_statPop   != null) _statPop.text   = $"{district.population:F0}k";
            if (_statInfra != null) _statInfra.text = $"{district.infrastructure:F0}";
            if (_statSust  != null) _statSust.text  = $"{district.sustainability:F0}";
            if (_statDebt  != null) _statDebt.text  = $"{district.debt:F0}";

            int crisisTicks = district.ticksAtDebtCap + district.ticksBelowHappiness20;
            int crisisFreeCount = SimulationConstants.TOTAL_TICKS - crisisTicks;
            if (_crisisFree != null) _crisisFree.text = $"{crisisFreeCount} / {SimulationConstants.TOTAL_TICKS}";
        }

        public void Hide()
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }

        private void ReturnToMenu()
        {
            if (GameManager.Instance == null) return;
            var player = GetComponent<Player>();
            GameManager.Instance.RequestQuitRpc(player != null ? player.NetworkObjectId : 0);
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
            return      new Color(0.87f, 0.42f, 0.42f);            // red
        }
    }
}
