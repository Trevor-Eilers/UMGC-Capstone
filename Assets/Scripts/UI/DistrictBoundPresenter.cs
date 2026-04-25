using Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public interface IDistrictBoundViewModel
    {
        void BindToPanel(VisualElement root);
        void UpdateFromDistrictState(DistrictState state);
    }

    public abstract class DistrictBoundPresenter<TViewModel> : UIPresenterBase
        where TViewModel : ScriptableObject, IDistrictBoundViewModel
    {
        protected TViewModel viewModel;

        public void Initialize(Player player)
        {
            AcquireRoot();

            viewModel = ScriptableObject.CreateInstance<TViewModel>();
            root.dataSource = viewModel;
            viewModel.BindToPanel(root);

            OnViewModelBound(player);

            // Subscribe immediately if districtNetRef is already populated (e.g. for
            // late-joining clients or when this presenter is added after the network
            // value has already been set). Otherwise OnValueChanged would never fire
            // for an already-set value and this presenter would silently never receive
            // tick updates.
            var existingDistrict = player.District;
            if (existingDistrict != null)
            {
                existingDistrict.state.OnValueChanged -= OnDistrictStateChanged;
                existingDistrict.state.OnValueChanged += OnDistrictStateChanged;
            }

            player.districtNetRef.OnValueChanged += (_, _) =>
            {
                var district = player.District;
                if (district == null) return;
                district.state.OnValueChanged -= OnDistrictStateChanged;
                district.state.OnValueChanged += OnDistrictStateChanged;
            };
        }

        protected virtual void OnViewModelBound(Player player) { }

        private void OnDistrictStateChanged(DistrictState _, DistrictState newState)
            => viewModel.UpdateFromDistrictState(newState);

        protected virtual void OnDestroy()
        {
            if (viewModel != null) Destroy(viewModel);
        }
    }
}
