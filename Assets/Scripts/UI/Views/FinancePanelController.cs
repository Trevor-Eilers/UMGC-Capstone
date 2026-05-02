using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class FinancePanelController : DistrictBoundPresenter<TopBarViewModel>
    {
        private Label _green;
        private Label _transit;
        private Label _life;
        private Label _dev;

        private static readonly string[] InfoIconNames =
        {
            "RevenueInfo", "SpendingInfo", "CashFlowInfo", "ReserveInfo",
            "DebtInfo", "GrantsInfo",
            "GrantGreenBadge", "GrantTransitBadge",
            "GrantLifeBadge", "GrantDevBadge"
        };

        private void Start()
        {
            AcquireRoot();
            
            _green = root.Q<Label>("GrantGreenBadge");
            _transit = root.Q<Label>("GrantTransitBadge");
            _life = root.Q<Label>("GrantLifeBadge");
            _dev = root.Q<Label>("GrantDevBadge");
            
            var tooltip = new Tooltip(root);
            foreach (var iconName in InfoIconNames)
            {
                var icon = root.Q<VisualElement>(iconName);
                if (icon == null || string.IsNullOrEmpty(icon.tooltip)) continue;
                var capturedIcon = icon;
                icon.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    var anchor = new Vector2(capturedIcon.worldBound.xMax,
                        capturedIcon.worldBound.y);
                    var pos = root.WorldToLocal(anchor);
                    tooltip.Show(capturedIcon.tooltip, pos, new Vector2(20, 0));
                });
                icon.RegisterCallback<MouseLeaveEvent>(_ => tooltip.Hide());
            }
        }

        protected override void OnViewModelBound(Player player)
        {
            
            viewModel.BindToPanel(root);
            Refresh();
            viewModel.propertyChanged += (_, _) => Refresh();
        }

        void Refresh()
        {
            // A streak > 0 alone is not enough — during stabilization (debt >= 70)
            // grantsEligible is false and no grant pays out even if the threshold
            // condition is met. Gate on both so the badge reflects actual revenue.
            bool eligible = viewModel.GrantsEligible;
            SetBadge(_green,   eligible && viewModel.GreenGrantStreak   > 0);
            SetBadge(_transit, eligible && viewModel.TransitGrantStreak > 0);
            SetBadge(_life,    eligible && viewModel.LifeGrantStreak    > 0);
            SetBadge(_dev,     eligible && viewModel.DevGrantStreak     > 0);
        }
        
        private static void SetBadge(Label badge, bool active)
        {
            if (badge == null) return;
            if (active)
            {
                badge.RemoveFromClassList("grant-badge-off");
                badge.AddToClassList("grant-badge-on");
            }
            else
            {
                badge.RemoveFromClassList("grant-badge-on");
                badge.AddToClassList("grant-badge-off");
            }
        }
    }
}
