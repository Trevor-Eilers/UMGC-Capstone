using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    // Small view-side glue for the left-side Finance panel.
    //
    // Most values (Revenue, Spending, Budget, Reserve, Debt, deltas) bind
    // themselves via the existing DataBinding + converter plumbing. This
    // helper handles the two things that aren't a one-to-one binding:
    //
    //   1. Grant badges — CSS class toggle driven by whether each grant
    //      streak is > 0 this tick.
    //   2. Info-icon tooltips — (i) icons next to each row that pop a
    //      dark popup to the RIGHT of the icon on hover, matching the
    //      Policies panel pattern but mirrored (Finance is left of map).
    //
    // Called from Player.Start() once the UIDocument root is available.
    public static class BottomBarView
    {
        // (i) icons next to each row label + the four grant badges — all
        // share the same hover-tooltip behaviour. The badges carry their
        // own tooltip strings (what each grant does and how much it pays)
        // so a player can see the rules without opening the Help screen.
        private static readonly string[] InfoIconNames =
        {
            "RevenueInfo", "SpendingInfo", "BudgetInfo", "ReserveInfo",
            "DebtInfo", "GrantsInfo",
            "GrantGreenBadge", "GrantTransitBadge",
            "GrantLifeBadge", "GrantDevBadge"
        };

        public static void BindToPanel(VisualElement root, TopBarViewModel vm)
        {
            if (root == null || vm == null) return;

            var green   = root.Q<Label>("GrantGreenBadge");
            var transit = root.Q<Label>("GrantTransitBadge");
            var life    = root.Q<Label>("GrantLifeBadge");
            var dev     = root.Q<Label>("GrantDevBadge");

            void Refresh()
            {
                SetBadge(green,   vm.GreenGrantStreak   > 0);
                SetBadge(transit, vm.TransitGrantStreak > 0);
                SetBadge(life,    vm.LifeGrantStreak    > 0);
                SetBadge(dev,     vm.DevGrantStreak     > 0);
            }

            Refresh();
            vm.propertyChanged += (_, _) => Refresh();

            // Info-icon hover tooltips. The tooltip pops to the RIGHT of the
            // icon so it floats over the map rather than off-screen to the
            // left of the Finance panel.
            var tooltip = new TooltipController(root);
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
