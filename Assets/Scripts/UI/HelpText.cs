namespace UI
{
    // Player-facing prose for each HUD element. The tooltip strings on the
    // various UXML files should match the first line of the matching Body here
    // — they're the one-sentence answer to "what does this do?" The Body is
    // what the How-to-Play modal shows when the player wants to understand
    // the full mechanic, including thresholds and interactions.
    public static class HelpText
    {
        public readonly struct Entry
        {
            public readonly string Title;
            public readonly string Body;
            public Entry(string title, string body) { Title = title; Body = body; }
        }

        // ── Policies ──

        public static readonly Entry[] Policies = new Entry[]
        {
            new("Tax Rate",
                "How much you tax residents, from 5% to 30%. Raising it brings in more revenue — but residents get unhappy and the economy grows slower the higher it gets. The default 15% is a rough break-even for a balanced budget."),

            new("Education",
                "Spending on schools and workforce training. This is the single strongest lever on GDP growth. If you want a booming economy, keep this high; if you cut it, GDP will slowly erode."),

            new("Infrastructure",
                "Spending on roads, utilities, and public works. Builds up your Infrastructure metric, which supports both GDP and sustainability. Infrastructure decays every tick, so you need ongoing investment just to hold steady around 50."),

            new("Housing",
                "Spending on residential amenities. The direct dial for happiness — and the biggest factor in whether new residents pick your district when they move in from the metro. You can keep happiness high with housing even if your district is otherwise struggling, but only for so long."),

            new("Environment",
                "Spending on pollution control and environmental regulation. Supports long-term sustainability (which keeps population from fleeing). If you drop below 30% while your GDP is above 40, pollution starts spreading to your neighbors — and back to you."),

            new("City Contribution",
                "How much you chip into shared metro projects — transit, highways, public facilities. Unlike the other spending, this benefits everyone. It's required for cross-district commuting to work and is worth 25% of the City Contribution score. Don't be the free-rider.")
        };

        // ── District metrics ──

        public static readonly Entry[] District = new Entry[]
        {
            new("GDP",
                "Economic output index, 0–100. Grows from education spending and a strong Infrastructure metric. Held back by high taxes, heavy environmental regulation, and natural decay. Drives your tax revenue — the higher your GDP, the more money each percentage point of tax produces."),

            new("Happiness",
                "Resident satisfaction, 0–100. About 60% of it comes from your district's overall health (GDP, infra, sustainability, low debt); the other 40% is direct effects from housing spending (up) and tax rate (down). Stay above 20 — below that, ticks count toward a crisis penalty on your final score."),

            new("Population",
                "Residents in thousands (e.g. 150k). You don't set this directly. New residents arrive each tick based on city reputation and how attractive your district is (happiness, housing spending, low taxes). If your sustainability drops below 30, existing residents start leaving."),

            new("Infrastructure",
                "Quality of your physical infrastructure, 0–100. Grows from infrastructure spending with diminishing returns above 50, decays naturally every tick. Drives your GDP and sustainability."),

            new("Sustainability",
                "Long-term carrying capacity of your district, 0–100. Driven mostly by your infrastructure level and environment spending, drained by population pressure. If it falls below 30, residents start leaving — and fewer residents make the problem worse."),

            new("Debt",
                "How far underwater you are, 0–80. Accrues when you deficit-spend, pays down when you surplus. At 40, residents start getting stressed (happiness penalty). At 60 (the debt cap), the game starts scaling down your effective spending until you get back in the black. At 70, federal stabilization kicks in but you lose access to grants."),

            new("Budget",
                "Revenue minus spending, this tick. Positive means you have a surplus — it pays down debt first, then fills your reserve. Negative means you have a deficit — it drains your reserve first, then accrues debt. Reserve decays slightly each tick, so you can't hoard forever."),

            new("Revenue",
                "Tax income this tick. Scales with your tax rate, your GDP, and your population. Both a bigger economy and a bigger population make every point of tax worth more."),

            new("Pollution",
                "How much pollution your district is generating. Kicks in only when your Environment spending is below 30% AND your GDP is above 40 — a polluting industrial economy. Damages neighbors' sustainability and happiness, and your own. Raise environment spending to shut it off.")
        };

        // ── City metrics ──

        public static readonly Entry[] City = new Entry[]
        {
            new("Reputation",
                "A weighted average of all districts' happiness, sustainability, infra, GDP, and fiscal health — minus a penalty for how unequal the city is. Above 70, new residents arrive in accelerating numbers. Below 30, they leave the metro entirely. Drives the single biggest slice of your City Contribution score (50%)."),

            new("Shared Infrastructure",
                "Metro-wide infrastructure quality (transit, highways, public facilities), 0–100. Grows from every player's City Contribution spending, decays without investment. Needed at 25+ for cross-district commuting to work. Your share of contributions is 25% of your City Contribution score."),

            new("Metro Inflow",
                "Net population flow into the city this tick, shown as a 0–100 scale where 50 is neutral. Above 50 means new residents are arriving (reputation is good); below 50 means residents are leaving the metro. Distributed across districts by happiness, housing, and taxes — so housing and happiness matter for your share.")
        };
    }
}
