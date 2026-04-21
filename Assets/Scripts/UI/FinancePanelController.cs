using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class FinancePanelController : DistrictBoundPresenter<TopBarViewModel>
    {
        private static readonly string[] InfoIconNames =
        {
            "RevenueInfo", "SpendingInfo", "BudgetInfo", "ReserveInfo",
            "DebtInfo", "GrantsInfo",
            "GrantGreenBadge", "GrantTransitBadge",
            "GrantLifeBadge", "GrantDevBadge"
        };

        protected override void OnViewModelBound(Player player)
        {
            AcquireRoot();
            viewModel.BindToPanel(root);

            var green   = root.Q<Label>("GrantGreenBadge");
            var transit = root.Q<Label>("GrantTransitBadge");
            var life    = root.Q<Label>("GrantLifeBadge");
            var dev     = root.Q<Label>("GrantDevBadge");

            void Refresh()
            {
                SetBadge(green,   viewModel.GreenGrantStreak   > 0);
                SetBadge(transit, viewModel.TransitGrantStreak > 0);
                SetBadge(life,    viewModel.LifeGrantStreak    > 0);
                SetBadge(dev,     viewModel.DevGrantStreak     > 0);
            }

            Refresh();
            viewModel.propertyChanged += (_, _) => Refresh();

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
