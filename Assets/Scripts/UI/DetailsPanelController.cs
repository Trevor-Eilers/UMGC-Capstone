// Author: Trevor Eilers

using UnityEngine.UIElements;

namespace UI
{
    // Thin Presenter glue for the Details panel. The base class
    // DistrictBoundPresenter<DetailsPanelViewModel> already:
    //   • creates the DetailsPanelViewModel ScriptableObject,
    //   • wires it as root.dataSource for UIBuilder bindings,
    //   • calls viewModel.BindToPanel(root), and
    //   • subscribes to the local player's District.state updates so each tick
    //     flows through viewModel.UpdateFromDistrictState.
    //
    // This subclass adds overlay show/hide behavior and exposes a single public
    // entry point (ShowFinalScore) that GameManager.EndGameRpc invokes once at
    // end of game to hand off the ScoringSystem.ComputeFinalScore result.
    public class DetailsPanelController : DistrictBoundPresenter<DetailsPanelViewModel>
    {
        private VisualElement _overlay;

        protected override void OnViewModelBound(Player player)
        {
            _overlay = root.Q<VisualElement>("DetailsOverlay");
            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;

            viewModel.OnCloseRequested += HideOverlay;
        }

        private void HideOverlay()
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }

        // Called once by GameManager.EndGameRpc after ScoringSystem.ComputeFinalScore
        // has produced this player's final numbers. Forwards to the VM which stores
        // and notifies bindings — no calculation happens in the Presenter.
        public void ShowFinalScore(FinalScore score, float citySpendSharePercent)
        {
            if (viewModel == null) return;
            viewModel.SetFinalScore(score, citySpendSharePercent);
        }
    }
}
