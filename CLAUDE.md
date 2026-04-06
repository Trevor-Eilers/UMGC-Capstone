# CivicEngine — Simulation Engine Specification

## Project Context

CivicEngine is a multiplayer tick-based city-economy simulation game built in Unity (C#)
for the CMSC 495 Computer Science Capstone at UMGC, Spring 2026. Two to four players each
govern a district within a shared metropolitan region. Players set policy sliders that the
simulation engine resolves each tick. The core design goal is making the tension between
local optimization and collective city-wide outcomes tangible through gameplay.

The authoritative design document is the Project Design Document (PDD):
`CMSC_495_Project_Design_3.docx`. This CLAUDE.md translates the PDD's design into
implementable formulas and architecture constraints for the simulation engine.

---

## Architecture Rules

These are non-negotiable constraints from the PDD. Do not deviate from them.

1. **Stateless pure functions.** The Tick Processor is implemented as a C# class containing
   static pure functions. It receives the current GameState as input and returns updated
   metric values as output. No side effects, no persistent state within the processor.

2. **No Unity dependencies in simulation logic.** The core math lives in plain C# classes
   that can be unit tested without the Unity editor. Unity's main game loop calls these
   functions synchronously at each tick boundary. No MonoBehaviour inheritance, no
   coroutines, no Update() loops in the simulation layer.

3. **Synchronous and deterministic.** All tick resolution is synchronous. No background
   threads, no async operations, no randomness. Given identical GameState and PolicyInputs,
   the Tick Processor must always produce identical output. This is critical for multiplayer
   desync prevention.

4. **Session owner is the sole authority.** The host instance (session owner) runs the Tick
   Processor and broadcasts updated state to all clients. Clients display state but never
   compute it. Policy slider changes from clients are submitted via RPCs and written to
   GameState only on the host. The existing codebase uses Unity Distributed Authority mode
   (`WithDistributedAuthorityNetwork()`), so enforce authority conventions in code: the
   GameState object should be owned by the session owner with
   `NetworkVariableWritePermission.Owner`.

5. **Tick timing.** 12 ticks per simulated month, 48 months, 576 total ticks. At 1x speed,
   one tick fires every ~3.1 seconds for a ~30 minute session. At 3x speed, one tick every
   ~0.78 seconds. The engine must resolve ALL districts, spillover, city metrics, and
   federal funding within that window including network round-trip.

---

## Existing Codebase Context

The repo is at `github.com/Trevor-Eilers/UMGC-Capstone`. As of Sprint 1 start, Trevor
has built:

- `ConnectionManager.cs` — Session creation/joining via Unity Multiplayer Services with
  anonymous auth. Uses Distributed Authority networking mode.
- `LobbyManager.cs` — NetworkBehaviour tracking player names via NetworkList, handles
  join/leave, transitions to MainScene when host clicks Start. RemovePlayerRpc is still
  a stub.
- `LobbyUI.cs` / `MainMenu.cs` — UI Toolkit lobby with display name, session name, join
  button, player list, host-only start button.
- `PolicyValues.cs` — Reads all six sliders (Tax, Edu, Infra, Housing, Env, City) into
  local floats via change callbacks. These are LOCAL ONLY — plain float fields on a
  MonoBehaviour, not NetworkVariables. They need to be converted to RPC submissions to
  the host's GameState.
- `PolicyBarWidget.cs` / `IndicatorWidget.cs` — Custom UI Toolkit elements. All six
  sliders with 0-100 ranges. Indicator widget has Label + Value display.
- Two scenes: `MainMenuScene` (lobby flow) and `MainScene` (game, currently empty).
- Note: "Environment" is misspelled as "Enivronment" in PolicyBarWidget.uxml.

No simulation logic exists yet. The simulation engine is being built from scratch.

---

## Data Structures

These are the API contracts between the simulation engine and the Unity client/networking
layer. Field names, types, and valid ranges are authoritative.

### DistrictState

Represents one player's district. Four of these exist in GameState.

```
DistrictState:
    int    playerId             // which player owns this district (0-3)
    PolicySliders sliders       // current slider positions (set by player)
    float  gdp                  // 0-100, normalized economic productivity index
    float  happiness            // 0-100, normalized resident satisfaction index
    float  population           // absolute count in thousands (e.g., 150.0 = 150k residents)
    float  infrastructure       // 0-100, normalized physical infrastructure quality index
    float  sustainability       // 0-100, normalized long-term carrying capacity index
    float  debt                 // 0-80, where 60 is the debt cap threshold, 80 is theoretical max
    float  reserve              // 0-RESERVE_CAP, fiscal reserve buffer in budget units
    float  revenue              // computed each tick, not set by player (budget units)
    float  totalSpending        // computed each tick, not set by player (budget units)
    float  scaleFactor          // computed each tick, 1.0 unless at debt cap (0.0-1.0)
    int    greenGrantStreak     // consecutive ticks receiving green infrastructure grant
    int    transitGrantStreak   // consecutive ticks receiving federal transit grant
    int    lifeGrantStreak      // consecutive ticks receiving quality of life grant
    int    devGrantStreak       // consecutive ticks receiving development grant
    bool   grantsEligible       // false when receiving stabilization transfers
    int    ticksAtDebtCap       // cumulative ticks spent at debt >= 60 (for scoring)
    int    ticksBelowHappiness20 // cumulative ticks spent at happiness < 20 (for scoring)
    float  totalCitySpending    // cumulative city contribution spending across all ticks (for scoring)
```

### PolicySliders

The six adjustable policy levers. Set by the player, read by the simulation.

```
PolicySliders:
    float taxRate          // 5-30, percentage. Generates revenue. NOT a spending slider.
    float education        // 0-100, percentage of max allocation. Costs budget, grows GDP.
    float infrastructure   // 0-100, percentage of max allocation. Costs budget, builds infra.
    float housing          // 0-100, percentage of max allocation. Costs budget, boosts happiness.
    float environment      // 0-100, percentage of max allocation. Costs budget, supports sustainability.
    float cityContribution // 0-50, percentage of max allocation. Costs budget, feeds shared city infra.
```

### CityMetrics

City-wide values shared across all districts. Computed by CityMetricsManager.

```
CityMetrics:
    float cityReputation       // 0-100, weighted average of district metrics minus variance penalty
    float sharedInfraQuality   // 0-100, persistent metric driven by collective city contribution spending
    float metroPopulationPool  // per-tick flow of new residents entering the metro (can be negative)
```

### GameState

The top-level container. This is what the Tick Processor receives and returns.

```
GameState:
    DistrictState[4]  districts       // one per player/quadrant
    CityMetrics       cityMetrics     // city-wide shared state
    int               currentTick     // 0-575
    int               currentMonth    // 0-47 (derived: currentTick / 12)
    float             gameSpeed       // 0 (paused), 1, 2, or 3
    bool              isPaused        // host-controlled
    int               numActivePlayers // 2-4, only active districts participate in calculations
```

---

## Starting Values

All districts begin identical. These values are from the PDD and are chosen so that:
- Mid-range starting point gives players freedom to specialize in any direction.
- Cross-border spillover mechanics activate early as strategies diverge (commuting triggers
  at GDP differential > 5, gentrification at > 8).
- Happiness and sustainability start slightly above other indices to provide a buffer before
  negative consequences trigger (crisis at happiness < 20, outmigration at sustainability < 30).
- Default slider positions produce a balanced budget (revenue = spending) so a new player
  who doesn't adjust sliders won't accumulate debt during the first few ticks.

```
GDP_START               = 50
HAPPINESS_START         = 55
POPULATION_START        = 150.0    // 150k residents
INFRASTRUCTURE_START    = 50
SUSTAINABILITY_START    = 55
DEBT_START              = 15
RESERVE_START           = 0
SHARED_INFRA_START      = 50       // persistent; starts mid-range so commuting is possible from tick 1
TAX_RATE_DEFAULT        = 15       // 15% — midpoint gives room to cut (5%) or raise (30%)
SLIDERS_DEFAULT         = 50       // all spending sliders at 50%
CITY_CONTRIB_DEFAULT    = 25       // city contribution at 50% of its 0-50 range
```

---

## Tick Resolution Pipeline

Every tick resolves in five sequential phases. Order matters — each phase depends on
outputs of the previous one. No phase reads forward. This is what makes the Tick Processor
implementable as a single synchronous pass with no circular dependencies.

```
Phase 1: Budget Resolution    (revenue, spending, debt cap scaling, reserve/debt flow)
Phase 2: Local Effects         (what each slider's actual spend produces in metric deltas)
Phase 3: Spillover             (cross-district interactions based on metric differentials)
Phase 4: City Metrics          (reputation, population distribution, shared infra, federal funding)
Phase 5: Clamp and Commit      (enforce valid ranges, update cumulative tracking fields)
```

---

## Phase 1: Budget Resolution

This phase determines how much money the district has (revenue), how much it wants to
spend (spending demand), whether the debt cap forces spending down (scale factor), and
how the budget balance flows into reserve or debt.

### Step 1.1 — Revenue

```
revenue = (taxRate / 100) * GDP * (population / 1000) * K_REV
```

**Design rationale:** Three inputs multiply together. Tax Rate is the player's direct
fiscal lever (5-30%). GDP represents economic productivity — a wealthier district generates
more tax revenue per capita per percentage point of taxation. Population is the tax base —
more residents means more taxpayers. K_REV is a global scaling constant controlling the
absolute size of budget numbers.

At starting values: `revenue = 0.15 * 50 * 150 * 1.0 = 1125`

**RISK — GDP-revenue feedback loop:** Higher GDP → more revenue → more spending capacity →
more education spending → higher GDP. This loop is intentional (economic growth should
compound) but must be braked by: GDP diminishing returns `(1 - GDP/100)`, natural GDP
decay, and tax drag proportional to GDP level. If one player consistently runs away with
GDP during playtesting, increase K_GDP_DECAY or the tax drag coefficient.

### Step 1.2 — Spending Demand

Each spending slider generates a cost proportional to the slider position and population.
Bigger districts cost more to run (more students, more roads, more housing).

```
eduCost     = (education / 100)        * (population / 1000) * K_SPEND
infraCost   = (infrastructure / 100)   * (population / 1000) * K_SPEND
housingCost = (housing / 100)          * (population / 1000) * K_SPEND
envCost     = (environment / 100)      * (population / 1000) * K_SPEND
cityCost    = (cityContribution / 50)  * (population / 1000) * K_SPEND * K_CITY_WEIGHT

totalSpending = eduCost + infraCost + housingCost + envCost + cityCost
```

**Why divide cityContribution by 50 instead of 100:** City Contribution's slider range is
0-50, not 0-100 like the others. Dividing by 50 normalizes it to the same 0.0-1.0 scale
so the cost calculation is consistent. K_CITY_WEIGHT is a separate multiplier allowing
city spending to be tuned cheaper or more expensive than domestic spending. At 1.0, they
cost the same per normalized unit.

At starting values (all sliders 50, city contribution 25):
```
totalSpending = (0.5 + 0.5 + 0.5 + 0.5) * 150 * 3.0 + (25/50) * 150 * 3.0 * 1.0
             = 900 + 225 = 1125
```

Revenue (1125) equals spending (1125). Balanced budget at defaults. This is by design —
a new player who doesn't touch anything won't accumulate debt.

**RISK — Population scaling on both sides:** Revenue and spending both scale with
population, so they roughly cancel. But if K_REV and K_SPEND aren't calibrated precisely,
population growth could be systematically budget-positive (encouraging growth at all costs)
or budget-negative (punishing growth). During calibration, verify that a player who grows
population from 150k to 250k without changing sliders still has a roughly balanced budget.

### Step 1.3 — Debt Cap Scaling

When debt reaches the cap (60), the district can no longer deficit-spend. All spending is
proportionally scaled down to match available revenue. The player's slider RATIOS are
preserved — if Education was set twice as high as Housing, Education still gets twice the
actual spend — but everything delivers less.

```
if debt >= DEBT_CAP:
    if totalSpending > revenue:
        scaleFactor = revenue / totalSpending    // e.g., 0.70 means 70% effectiveness
    else:
        scaleFactor = 1.0                        // revenue covers spending, no scaling needed
else:
    scaleFactor = 1.0                            // not at cap, full spending

actualEduCost     = eduCost     * scaleFactor
actualInfraCost   = infraCost   * scaleFactor
actualHousingCost = housingCost * scaleFactor
actualEnvCost     = envCost     * scaleFactor
actualCityCost    = cityCost    * scaleFactor
actualTotalSpending = totalSpending * scaleFactor
```

**Design rationale:** This is the core punishment for debt. It doesn't lock the player out
of any slider or change their settings. It just makes everything less effective. A player
spending 1500 with revenue 1000 gets scaleFactor = 0.67 — every slider operates at 67%.
The only way to restore full effectiveness is to raise taxes (more revenue), lower sliders
(less demand), or both.

**RISK — Cliff dynamics:** At debt 59, full spending power. At debt 60, sudden drop to
(potentially) 70% effectiveness. The debt stress happiness penalty starting at debt 40 and
the visual debt warning at debt 48 (80% of cap) provide advance warning. If playtesting
shows players still getting blindsided, consider softening:
```
// Optional softer version — scaling begins at debt 50, fully active at 60
if debt >= 50:
    softenFactor = (debt - 50) / 10.0   // 0.0 at debt 50, 1.0 at debt 60
    scaleFactor = lerp(1.0, revenue / totalSpending, softenFactor)
```
Start with the hard cap for Sprint 1. Simpler to implement and reason about.

### Step 1.4 — Budget Balance → Reserve → Debt

After debt cap scaling, compute the actual budget balance and flow it through the
reserve/debt priority chain.

**Priority rules:**
- Surplus → pay down debt first → then fill reserve
- Deficit → drain reserve first → then accrue debt
- A player can NEVER fill reserve while carrying debt
- A player can NEVER accrue debt while holding reserve
- 3:1 asymmetry: debt accrues 3x faster than it recovers (deficit spending is a commitment)

```
budgetBalance = revenue - actualTotalSpending

if budgetBalance >= 0:
    // ── SURPLUS ──
    if debt > 0:
        // Pay down debt first
        maxDebtReduction = budgetBalance * K_DEBT_RECOVERY
        debtReduction = min(maxDebtReduction, debt)
        debt -= debtReduction

        // Whatever surplus remains after debt service fills the reserve
        surplusUsedForDebt = debtReduction / K_DEBT_RECOVERY
        surplusRemaining = budgetBalance - surplusUsedForDebt
        reserve = min(reserve + surplusRemaining, RESERVE_CAP)
    else:
        // No debt — all surplus flows into reserve
        reserve = min(reserve + budgetBalance, RESERVE_CAP)
else:
    // ── DEFICIT ──
    deficit = abs(budgetBalance)

    if reserve > 0:
        // Drain reserve before touching debt
        absorbed = min(deficit, reserve)
        reserve -= absorbed
        deficit -= absorbed

    if deficit > 0:
        // Remaining deficit accrues as debt
        debt = clamp(debt + deficit * K_DEBT_ACCRUAL, 0, 80)
```

**Reserve decay:** The reserve loses a small percentage each tick to prevent indefinite
hoarding. A player who taxes at 25% for 100 ticks to build a fat reserve has been eating
happiness and GDP penalties the entire time — that sacrifice justifies the cushion. But
without decay, the reserve could sit untouched for hundreds of ticks, which breaks pacing.

```
reserve = reserve * (1.0 - K_RESERVE_DECAY)    // applied each tick before budget balance
```

With K_RESERVE_DECAY = 0.005 (0.5% per tick), a full reserve of 22500 loses ~112 per tick.
Over 100 ticks that's significant — the player must use the reserve within a reasonable
window or it erodes.

**RISK — Reserve trivializes debt:** A patient player could tax high, build max reserve,
then drop taxes and ride the cushion. The tax penalties running during accumulation are the
cost. If the "save then spend" strategy still dominates in playtesting, lower RESERVE_CAP
or increase K_RESERVE_DECAY.

---

## Phase 2: Local Effects

Each slider's actual spend (post-debt-cap scaling) converts to metric deltas. These are
computed independently per district — no cross-district interactions.

### 2.1 — GDP

GDP has four growth inputs, two drags, and natural decay. This gives players multiple paths
to economic growth rather than making education the only lever.

```
// ── GROWTH INPUTS ──

// Education — primary growth driver. Direct from spending.
// This is the most responsive GDP lever: spend more education, see GDP rise.
gdpGrowth_edu = actualEduCost * K_EDU_TO_GDP

// Infrastructure level — physical capital contribution.
// Driven by the infrastructure METRIC (0-100), not spending directly.
// Centered on 50: infrastructure above 50 boosts GDP, below 50 drags it.
// This creates a lagged effect: infra spending → infra metric → GDP next tick.
gdpGrowth_infra = (infrastructure - 50) * K_INFRA_TO_GDP

// Population — labor pool with diminishing returns.
// More residents = more workers = more output, but congestion limits gains.
// Log function ensures first 100k matters much more than going 400k → 500k.
// GUARD: log(0) is -infinity. Clamp to minimum 1.0 (representing 1k).
gdpGrowth_pop = log(max(population / 1000, 1.0)) * K_POP_TO_GDP

// Sustainability — business environment signal.
// High sustainability = attractive to business/talent. Low = signals decline.
// Centered on 50: above 50 boosts GDP, below 50 drags it.
// Creates feedback: infra neglect → low sustainability → GDP drag → less revenue.
gdpGrowth_sustain = (sustainability - 50) * K_SUSTAIN_TO_GDP

// ── DRAGS ──

// Tax drag — higher taxes slow economic growth.
// PROPORTIONAL TO CURRENT GDP: taxing a booming economy hurts more than taxing
// a struggling one. This is a natural brake on runaway GDP.
gdpDrag_tax = -(taxRate / 100) * GDP * K_TAX_GDP_DRAG

// Environment regulation drag — environmental rules impose economic costs.
// Driven by actual environment spending, not slider position.
// The sustainability benefit (via K_ENV_TO_SUSTAIN) partially offsets this long-term.
gdpDrag_env = -actualEnvCost * K_ENV_GDP_DRAG

// ── DECAY ──

// Natural decay — an economy without ongoing investment stagnates.
// Without education spending, GDP erodes over time.
gdpDecay = GDP * K_GDP_DECAY

// ── AGGREGATE WITH DIMINISHING RETURNS ──

totalGdpDelta = gdpGrowth_edu
              + gdpGrowth_infra
              + gdpGrowth_pop
              + gdpGrowth_sustain
              + gdpDrag_tax
              + gdpDrag_env
              - gdpDecay

// Diminishing returns on POSITIVE growth only.
// At GDP 50, positive growth is halved. At GDP 90, it's reduced to 10%.
// GDP asymptotically approaches 100 but never reaches it.
// Negative deltas (decay, drags) are NOT diminished — decline is always at full speed.
if totalGdpDelta > 0:
    totalGdpDelta = totalGdpDelta * (1.0 - GDP / 100.0)

GDP = clamp(GDP + totalGdpDelta, 0, 100)
```

**Target weighting during calibration (approximate contribution to GDP growth):**
- Education: ~40% of GDP growth potential (strongest single lever)
- Infrastructure level: ~25% (meaningful but secondary)
- Population: ~20% (matters but diminishing)
- Sustainability: ~15% (subtle but real)

These aren't literal percentages in code — they're targets for K constant calibration.

**RISK — GDP oscillation:** Multiple inputs with feedback loops and delays can produce
oscillation (GDP spikes, overcorrects, drops, overcorrects). The diminishing return dampens
upward spikes. K_GDP_DECAY provides constant downward pull. Both are stabilizing. During
calibration, verify GDP converges to stable equilibrium at fixed slider positions.

**RISK — log(population) at zero:** Total outmigration could theoretically push population
to near-zero. The `max(population/1000, 1.0)` guard prevents math errors. A district at 1k
population is functionally dead — zero meaningful revenue or output.

### 2.2 — Happiness

Happiness uses a hybrid model (Option 3): a metric baseline reflecting district health
(~60% weight) plus direct slider effects (~40% weight). This means happiness is partially
"earned" by building a good district and partially "bought" through housing investment.

A player CAN maintain high happiness with heavy housing spending even if their district
is structurally struggling — but only temporarily. When sustainability drops and population
flees, the metric baseline drags happiness down regardless of housing spending.

```
// ── METRIC BASELINE ──
// Reflects overall district health. Weighted average of four metrics.

inverseDebt = 100.0 - (debt * 100.0 / 80.0)    // maps debt 0-80 to score 100-0

metricBaseline = GDP            * W_HAPPY_GDP       // 0.30 — prosperity
               + infrastructure * W_HAPPY_INFRA     // 0.25 — physical quality of life
               + sustainability * W_HAPPY_SUSTAIN   // 0.25 — long-term district health
               + inverseDebt   * W_HAPPY_DEBT       // 0.20 — fiscal stability

// ── DIRECT EFFECTS ──

// Housing — the "make people happy now" button.
// This is the primary direct happiness lever. Spending on housing (nice apartments,
// affordable homes, residential amenities) directly boosts resident satisfaction.
// No diminishing returns — if you want to dump budget into happiness, that's valid
// (but the opportunity cost is real: every dollar in housing isn't in education/infra).
happinessDelta_housing = actualHousingCost * K_HOUSING_TO_HAPPY

// Tax penalty — higher taxes always hurt happiness. Linear, always active.
happinessDelta_tax = -(taxRate / 100.0) * K_TAX_HAPPY_PENALTY

// Debt stress — anxiety kicks in above debt 40. Provides early warning before
// the debt cap at 60. Stress at debt 45 is mild. At debt 58 it's severe.
// At debt 60+ the player is getting stress AND proportional spending scaling.
debtStress = max(0, debt - 40) * K_DEBT_STRESS

// ── COMBINED ──

happiness = metricBaseline * K_BASELINE_WEIGHT     // 0.60 — district health is 60%
          + happinessDelta_housing                  // direct housing boost
          + happinessDelta_tax                      // tax penalty (negative)
          - debtStress                              // debt anxiety (negative above 40)

happiness = clamp(happiness, 0, 100)
```

**IMPORTANT — Unit consistency:** The metric baseline produces values in 0-100 range. But
happinessDelta_housing is `actualHousingCost * K_HOUSING_TO_HAPPY` where actualHousingCost
is in budget units (potentially hundreds or thousands). K_HOUSING_TO_HAPPY must be small
enough that the housing boost produces values in roughly 0-50 range, not 0-5000. Same for
the tax penalty. During calibration, run the formula at extremes (best case: all metrics
100, max housing, 5% tax. Worst case: all metrics 0, zero housing, 30% tax, debt 80) and
verify output stays within 0-100.

**RISK — Happiness volatility:** Unlike GDP/infrastructure/sustainability which are delta-
based (change by some amount each tick), happiness is RECOMPUTED FROM SCRATCH each tick
based on current state. This makes it the most responsive metric — instant feedback for
the player. But it can swing wildly if inputs change fast. If volatility is a problem
during playtesting, add smoothing:
```
// Optional smoothing — blend toward target over multiple ticks
targetHappiness = (formula above)
happiness = happiness + (targetHappiness - happiness) * K_HAPPY_SMOOTHING
// K_HAPPY_SMOOTHING: 0.1 = very smooth (slow response), 1.0 = instant (no smoothing)
```

### 2.3 — Infrastructure

Infrastructure is a persistent metric that grows from spending and decays without it.
Roads deteriorate, buildings degrade, utilities age. Ongoing investment is required just
to maintain the current level.

```
// Growth from spending, with diminishing returns at high values.
// It's cheap to maintain infrastructure at 50 but expensive to push to 80+.
// A player chasing the development grant (infrastructure > 80) must dedicate
// significant budget, squeezing other spending.
infraGrowth = actualInfraCost * K_INFRA_TO_INFRA * (1.0 - infrastructure / 100.0)

// Natural decay — entropy. Without spending, infrastructure erodes.
infraDecay = infrastructure * K_INFRA_DECAY

// Net delta
infrastructure = clamp(infrastructure + infraGrowth - infraDecay, 0, 100)
```

**RISK — Maintenance treadmill:** If K_INFRA_DECAY is too high, players feel like they're
running just to stand still. All spending prevents decay rather than building anything.
If K_INFRA_DECAY is too low, infrastructure is a one-time investment — pump it up, then
redirect spending. The sweet spot: maintaining level 50 requires moderate ongoing
investment, leaving room to grow or accept gradual decline.

### 2.4 — Sustainability (Carrying-Capacity Model)

Sustainability is the most interconnected metric. It's driven by infrastructure (primary),
environment (secondary), and population drain. Its primary OUTPUT is population migration:
when sustainability drops below a critical threshold, residents leave.

This is the long-term tradeoff at the heart of CivicEngine: a player can maintain high
short-term happiness through housing spending while neglecting infrastructure and
environment, but sustainability will eventually collapse and trigger outmigration that
erodes the tax base.

```
// ── INPUTS ──

// Infrastructure as PRIMARY sustainability input.
// Driven by the infrastructure METRIC LEVEL (0-100), not spending directly.
// Centered on 50: above 50 contributes positively, below 50 negatively.
// At starting infra (50), this term is zero — neutral.
// Player must ACTIVELY INVEST above 50 to build sustainability.
sustainDelta_infra = (infrastructure - 50) * K_INFRA_TO_SUSTAIN

// Environment spending as SECONDARY input.
// Represents long-term benefits of environmental regulation: cleaner air,
// resource conservation, pollution control. Reinforces carrying-capacity
// mechanic without replacing infrastructure's central role.
// K_INFRA_TO_SUSTAIN should be 3-4x K_ENV_TO_SUSTAIN to enforce hierarchy.
sustainDelta_env = actualEnvCost * K_ENV_TO_SUSTAIN

// ── DRAINS ──

// Population pressure — larger districts drain sustainability faster.
// More people = more resource consumption = more strain on carrying capacity.
// A growing district needs proportionally more infrastructure investment just
// to maintain the same sustainability level.
popDrain = (population / 1000) * K_POP_SUSTAIN_DRAIN

// Natural entropy — sustainability decays without active maintenance.
sustainDecay = sustainability * K_SUSTAIN_DECAY

// ── NET DELTA ──

sustainDelta = sustainDelta_infra
             + sustainDelta_env
             - popDrain
             - sustainDecay

sustainability = clamp(sustainability + sustainDelta, 0, 100)

// ── MIGRATION TRIGGER ──
// When sustainability drops below the threshold, residents leave.
// The further below, the faster they leave. This is the death spiral mechanic:
// population loss → less revenue → harder to fund infrastructure →
// sustainability drops further → more population loss.
// The spiral DOES self-stabilize: fewer people = less popDrain = less pressure.
// But the equilibrium may be a small, depopulated district.

if sustainability < SUSTAIN_MIGRATION_THRESHOLD:
    migrationRate = (SUSTAIN_MIGRATION_THRESHOLD - sustainability) * K_MIGRATION_RATE
    population -= migrationRate
    population = max(population, MIN_POPULATION)    // floor at 1.0 (1k residents)
```

**RISK — Unrecoverable death spiral:** Once below threshold 30, population leaves, revenue
drops, infra funding dries up, sustainability drops further. The popDrain scaling with
population provides a natural brake (fewer people = less strain = sustainability can
recover). But verify during calibration that a district at sustainability 10, population
30k, with moderate infrastructure spending CAN actually recover. If it can't, the minimum
population floor (1k) prevents total collapse but the district is functionally dead. Add
a floor at 10k-20k if recovery should be possible from crisis states.

### 2.5 — Population (Local Effects Only)

Population is NOT directly controlled by any slider. It changes through two mechanisms:

1. **Sustainability-driven outmigration** (Phase 2, above) — residents leave when
   sustainability < 30.
2. **City-metrics-driven inflow** (Phase 4, below) — new residents arrive based on
   city reputation and district attractiveness.

This design is intentional. You can't BUY population. You can only create conditions
that attract or repel residents. Housing spending affects attractiveness (Phase 4) which
influences WHERE new residents settle, but not HOW MANY arrive at the metro level.

---

## Phase 3: Spillover

These mechanics fire after all local effects resolve. They operate on DIFFERENTIALS between
districts — the gap between neighbors creates the interaction. Process all pairs of
districts based on metric gaps.

### Adjacency Model

The city has four quadrants: NW, NE, SW, SE. In a four-player game, every district is
adjacent to every other (they all share the downtown center). But direct neighbors (sharing
a border) interact more strongly than diagonal neighbors (sharing only a corner).

```
// Direct neighbors (share a full border): full spillover weight
NW ↔ NE    (north border)     weight = 1.0
NW ↔ SW    (west border)      weight = 1.0
NE ↔ SE    (east border)      weight = 1.0
SW ↔ SE    (south border)     weight = 1.0

// Diagonal neighbors (share downtown corner only): half spillover weight
NW ↔ SE    (diagonal)         weight = 0.5
NE ↔ SW    (diagonal)         weight = 0.5
```

In 2-3 player games, only occupied district pairs participate.

### 3.1 — Gentrification

Triggers when GDP differential between two adjacent districts exceeds 8 points. The
wealthy district's economic activity raises property values in the poorer neighbor.

```
for each pair of adjacent districts (A, B):
    gdpDiff = A.gdp - B.gdp
    weight = adjacencyWeight(A, B)

    if abs(gdpDiff) > GENTRIFY_THRESHOLD:
        wealthy = A if gdpDiff > 0 else B
        poor    = A if gdpDiff < 0 else B
        magnitude = (abs(gdpDiff) - GENTRIFY_THRESHOLD) * weight

        // Effects on the POOR district:
        // Displacement stress — residents are priced out of their own neighborhood.
        poor.happiness  -= magnitude * K_GENTRIFY_HAPPY

        // Population displacement — some residents forced to relocate.
        poor.population -= magnitude * K_GENTRIFY_POP

        // Effects on the WEALTHY district:
        // Economic expansion — gentrification brings commercial investment.
        wealthy.gdp       += magnitude * K_GENTRIFY_GDP_GAIN

        // Congestion/inequality friction — rapid growth has social costs.
        // This is a partial brake on the GDP feedback loop.
        wealthy.happiness -= magnitude * K_GENTRIFY_WEALTHY_HAPPY
```

The magnitude scales linearly past the threshold. A 9-point gap (magnitude 1) is gentle.
A 25-point gap (magnitude 17) is aggressive. Gentrification pressure accelerates as
inequality grows.

**RISK — Compounding with commuting against weak players:** A district losing the GDP race
gets hit with BOTH commuting (GDP drain at gap 5) and gentrification (happiness/population
drain at gap 8). This could make recovery impossible. The classmate who flagged this was
right. If playtesting shows the losing player has no path back, consider: capping max
spillover magnitude per tick, adding a catch-up GDP bonus for districts far below city
average, or raising the gentrification threshold to 10-12 for more breathing room.

### 3.2 — Pollution Drift

Triggers when a district has environment spending below 30 AND GDP above 40 simultaneously.
Low environmental regulation plus industrial activity produces pollution.

```
for each district D:
    if D.sliders.environment < POLLUTE_ENV_THRESHOLD
       AND D.gdp > POLLUTE_GDP_THRESHOLD:

        // Pollution output — ADDITIVE formula for smoother gradient.
        // (Using multiplication would create extreme nonlinearity where pollution
        // is either negligible or devastating, with little middle ground.
        // Addition gives players more strategic granularity: environment at 20
        // produces meaningfully less pollution than environment at 10.)
        pollutionOutput = (max(0, POLLUTE_ENV_THRESHOLD - D.sliders.environment)
                        +  max(0, D.gdp - POLLUTE_GDP_THRESHOLD))
                        * K_POLLUTION_GENERATE

        // Pollution affects ALL adjacent neighbors
        for each neighbor N of D:
            weight = adjacencyWeight(D, N)
            N.sustainability -= pollutionOutput * K_POLLUTION_SUSTAIN * weight
            N.happiness      -= pollutionOutput * K_POLLUTION_HAPPY * weight

        // Source district also suffers (you live in your own pollution).
        // Self-damage should be LOWER than neighbor damage — pollution disperses
        // outward. Suggest K_POLLUTION_SELF_* at ~0.5x the neighbor constants.
        D.sustainability -= pollutionOutput * K_POLLUTION_SELF_SUSTAIN
        D.happiness      -= pollutionOutput * K_POLLUTION_SELF_HAPPY
```

**RISK — Pollution weaponization:** A player can set environment to 0, pump education for
high GDP, and flood neighbors with pollution while maintaining decent GDP. The self-damage
(via K_POLLUTION_SELF_SUSTAIN) is the primary deterrent — the polluter's own sustainability
takes hits, eventually triggering outmigration. If K_POLLUTION_SELF_SUSTAIN is too low,
the feedback is too slow to deter the strategy. Set self-damage at meaningful levels.

### 3.3 — Commuter Flows

Triggers when GDP differential between two districts exceeds 5 AND shared infrastructure
quality exceeds 25. Workers commute from lower-GDP to higher-GDP districts.

```
for each pair of adjacent districts (A, B):
    gdpDiff = A.gdp - B.gdp
    weight = adjacencyWeight(A, B)

    if abs(gdpDiff) > COMMUTE_GDP_THRESHOLD
       AND cityMetrics.sharedInfraQuality > COMMUTE_INFRA_THRESHOLD:

        work = A if gdpDiff > 0 else B      // high GDP — where jobs are
        home = A if gdpDiff < 0 else B      // low GDP — where workers live

        magnitude = (abs(gdpDiff) - COMMUTE_GDP_THRESHOLD) * weight

        // Better shared infrastructure = more commuting volume
        infraFactor = cityMetrics.sharedInfraQuality / 100.0

        commuters = magnitude * infraFactor * K_COMMUTE_VOLUME

        // Work district: gains economic output from commuters
        work.gdp        += commuters * K_COMMUTE_GDP_GAIN

        // Work district: congestion and crowding from influx
        work.happiness  -= commuters * K_COMMUTE_CONGESTION

        // Home district: economic activity happens elsewhere, GDP drains
        home.gdp        -= commuters * K_COMMUTE_GDP_DRAIN

        // Home district: employed residents are happier (they have jobs)
        home.happiness  += commuters * K_COMMUTE_HOME_HAPPY
```

**Three built-in brakes on the commuting feedback loop:**

1. **Shared infrastructure gating:** Commuting volume scales with sharedInfraQuality / 100.
   If players collectively underinvest in city contribution, commuting is suppressed
   regardless of GDP differentials.

2. **Congestion penalty:** The work district takes a happiness hit proportional to commuter
   volume. At high counts, the happiness cost outweighs the GDP gain in scoring (both
   are weighted at 22.5% of neighborhood score).

3. **GDP diminishing returns:** The global `(1 - GDP/100)` cap in Phase 2 applies to
   commuter GDP gains. The closer to GDP 100, the less each commuter is worth.

**RISK — Positive feedback loop:** Wealthy district gets GDP from commuters → wider gap →
more commuters. The brakes above should contain this, but it's the most likely source of
runaway dynamics. If one player consistently dominates via commuting, reduce
K_COMMUTE_GDP_GAIN or add a hard cap on commuters per tick.

---

## Phase 4: City Metrics

These resolve after all district-level effects. They operate on the city as a whole.

### 4.1 — City Reputation

Weighted average of five metrics across all districts, minus a variance penalty that
punishes inequality. Population is EXCLUDED to avoid a feedback loop (reputation drives
population inflow, so including population would create circularity).

```
// Average each metric across all ACTIVE districts
avgHappiness       = mean(all active districts' happiness)
avgSustainability  = mean(all active districts' sustainability)
avgInfrastructure  = mean(all active districts' infrastructure)
avgGDP             = mean(all active districts' GDP)
avgInverseDebt     = mean(all active districts' (100 - debt * 100/80))

// Weighted average — happiness and sustainability matter most
weightedAvg = avgHappiness      * 0.25
            + avgSustainability * 0.25
            + avgInfrastructure * 0.20
            + avgGDP            * 0.15
            + avgInverseDebt    * 0.15

// Variance penalty — measures inequality across districts.
// High standard deviation = unequal city = lower reputation.
// Four mediocre districts score higher than three great + one failing.
stdDevs = [stdev(all happiness values),
           stdev(all sustainability values),
           stdev(all infrastructure values),
           stdev(all GDP values),
           stdev(all inverseDebt values)]

variancePenalty = mean(stdDevs) * K_VARIANCE_PENALTY

cityReputation = clamp(weightedAvg - variancePenalty, 0, 100)
```

**RISK — Variance penalty with 2 players:** Standard deviation with only two data points
is volatile. Consider normalizing by player count or using a different dispersion measure
for small games.

**RISK — One player tanks reputation for everyone:** A single failing district drags all
averages down and inflates standard deviations. This is by design (collective
responsibility) but can frustrate strong players with no control over the weak one. The
crisis avoidance scoring component (25% of city score) penalizes the failing player
specifically, partially compensating.

### 4.2 — Metro Population Pool and Distribution

New residents enter the metro each tick based on city reputation, then get distributed
to districts based on attractiveness. The pool is a PER-TICK FLOW, not an accumulating
stock — residents are distributed immediately each tick.

```
// ── INFLOW CALCULATION ──

if cityReputation > 70:
    // Above 70: accelerating inflow — the city is thriving
    newResidents = (cityReputation - 70) * K_POP_INFLOW_HIGH
elif cityReputation >= 30:
    // 30-70: moderate inflow proportional to distance above 50
    // Below 50: still positive but very slow
    newResidents = (cityReputation - 50) * K_POP_INFLOW_NORMAL
else:
    // Below 30: net outflow — people are leaving the metro entirely
    newResidents = (cityReputation - 30) * K_POP_OUTFLOW    // negative value

// ── DISTRIBUTION TO DISTRICTS ──
// New residents choose a district based on attractiveness.
// Happiness (40%): people move where residents are satisfied.
// Housing investment (40%): people move where homes are available/affordable.
// Tax rate inverse (20%): people prefer lower-tax districts.

for each active district D:
    attractiveness_D = D.happiness * 0.40
                     + (D.sliders.housing / 100.0) * 0.40
                     + (1.0 - D.sliders.taxRate / 30.0) * 0.20

totalAttractiveness = sum(all attractiveness values)

// Guard against division by zero (all districts at zero attractiveness)
if totalAttractiveness > 0:
    for each active district D:
        share = attractiveness_D / totalAttractiveness
        D.population += newResidents * share
else:
    // Distribute equally if no district is attractive
    for each active district D:
        D.population += newResidents / numActivePlayers

// If newResidents is negative (outflow), the same distribution applies:
// the LEAST attractive districts lose the most population.
```

### 4.3 — Shared Infrastructure Quality (Persistent)

Shared infrastructure is a city-wide persistent metric that grows from collective city
contribution spending and decays without it. It behaves like district infrastructure —
requires ongoing investment to maintain.

```
// Sum all players' actual city contribution spending this tick
totalCitySpending = sum(all active districts' actualCityCost)

// Growth from collective investment
sharedInfraGrowth = totalCitySpending * K_SHARED_INFRA_GROWTH

// Decay — requires ongoing investment to maintain
sharedInfraDecay = sharedInfraQuality * K_SHARED_INFRA_DECAY

// Update
sharedInfraQuality = clamp(sharedInfraQuality + sharedInfraGrowth
                          - sharedInfraDecay, 0, 100)
```

**Why persistent:** Shared infrastructure represents physical transit systems, highways,
public facilities. These are built up over time with collective investment and deteriorate
without maintenance. A one-tick spike in city contribution shouldn't create instant
transit capacity, and one tick of zero spending shouldn't destroy it.

**RISK — One player carries the burden:** If one player sets city contribution to 50 while
others set 0, that player's spending might maintain shared infra above 25 alone. The free-
riders get commuting benefits without paying. This IS the tragedy of the commons working as
intended. The scoring system (shared infrastructure contributions = 25% of city score)
should deter free-riding. If it doesn't, increase contribution weight in scoring.

Shared infrastructure starts at 50 so commuting is possible from tick 1. If it started at
0, the first few months of gameplay would be purely local with no cross-district dynamics.

### 4.4 — Federal Funding: Competitive Grants

Four grants, each with a metric threshold. Available only to districts with debt below 60
(the cap). Consecutive awards in the same category diminish by 15% per tick to a 30% floor,
encouraging diversification.

```
for each active district D:
    grantRevenue = 0

    if D.debt < DEBT_CAP AND D.grantsEligible:

        // Green Infrastructure Grant — rewards sustainability investment
        if D.sustainability > 70:
            grantRevenue += GRANT_BASE_GREEN * max(0.30, 1.0 - D.greenGrantStreak * 0.15)
            D.greenGrantStreak += 1
        else:
            D.greenGrantStreak = 0

        // Federal Transit Grant — rewards population growth
        if D.population > 300.0:    // 300k
            grantRevenue += GRANT_BASE_TRANSIT * max(0.30, 1.0 - D.transitGrantStreak * 0.15)
            D.transitGrantStreak += 1
        else:
            D.transitGrantStreak = 0

        // Quality of Life Grant — rewards resident satisfaction
        if D.happiness > 75:
            grantRevenue += GRANT_BASE_LIFE * max(0.30, 1.0 - D.lifeGrantStreak * 0.15)
            D.lifeGrantStreak += 1
        else:
            D.lifeGrantStreak = 0

        // Development Grant — rewards infrastructure investment
        if D.infrastructure > 80:
            grantRevenue += GRANT_BASE_DEV * max(0.30, 1.0 - D.devGrantStreak * 0.15)
            D.devGrantStreak += 1
        else:
            D.devGrantStreak = 0

    // Grant revenue is bonus income — added to district revenue for next tick's budget
    // OR applied as direct debt reduction. Design decision: bonus revenue is simpler
    // and more useful (it funds spending). Apply as bonus revenue.
    D.revenue += grantRevenue
```

**Diminishing returns schedule:**
Tick 1: 100% → Tick 2: 85% → Tick 3: 70% → Tick 4: 55% → Tick 5: 40% → Tick 6+: 30%

**RISK — Grant thresholds unreachable or trivially easy:** Sustainability > 70, happiness
> 75, infrastructure > 80, population > 300k are all significantly above starting values.
During calibration, verify that a well-played district can reach 1-2 thresholds by mid-game
(~tick 250) and maybe 3 by late game. Four simultaneously should be rare.

**NOTE:** The PDD also mentions a fixed pool divided among qualifying players, adding a
competitive dynamic. This is deferred to Sprint 2. The per-district version above is
Sprint 1 scope.

### 4.5 — Federal Funding: Stabilization Transfers

Emergency lifeline for districts in deep debt crisis.

```
for each active district D:
    if D.debt >= 70:
        // Unconditional small debt reduction each tick
        D.debt -= K_STABILIZATION_RATE

        // Mutually exclusive with competitive grants
        D.grantsEligible = false
    else:
        // Re-enable grants once debt drops below cap
        if D.debt < DEBT_CAP:
            D.grantsEligible = true
```

This prevents total death spirals. A player at debt 75 gets automatic help, but they're
locked out of the grant reward system until they recover below 60.

**RISK — Stabilization prevents reaching debt 80:** If K_STABILIZATION_RATE exceeds max
possible debt accrual per tick, debt can never pass 70 + K_STABILIZATION_RATE. This may be
fine (80 is theoretical max, not a gameplay target). But if you want 80 to be reachable
(total collapse), set K_STABILIZATION_RATE below max per-tick accrual.

---

## Phase 5: Clamp and Commit

After all phases, enforce valid ranges and update tracking fields.

```
for each active district D:
    D.gdp              = clamp(D.gdp, 0, 100)
    D.happiness        = clamp(D.happiness, 0, 100)
    D.population       = clamp(D.population, MIN_POPULATION, MAX_POPULATION)
    D.infrastructure   = clamp(D.infrastructure, 0, 100)
    D.sustainability   = clamp(D.sustainability, 0, 100)
    D.debt             = clamp(D.debt, 0, 80)
    D.reserve          = clamp(D.reserve, 0, RESERVE_CAP)

    // Update cumulative tracking fields for end-of-game scoring
    if D.debt >= DEBT_CAP:
        D.ticksAtDebtCap += 1
    if D.happiness < 20:
        D.ticksBelowHappiness20 += 1
    D.totalCitySpending += D.actualCityCost    // lifetime city contribution

// City-level clamps
cityMetrics.cityReputation     = clamp(cityReputation, 0, 100)
cityMetrics.sharedInfraQuality = clamp(sharedInfraQuality, 0, 100)
```

**IMPORTANT — Clamp order:** Population is modified in Phase 2 (outmigration) AND Phase 4
(city inflow). Clamp ONCE here at the end, after all modifications. Don't clamp between
phases or you'll lose population silently.

```
MIN_POPULATION = 1.0      // 1k residents — functionally dead but prevents div-by-zero
MAX_POPULATION = 1000.0   // 1M residents — theoretical ceiling, may never be reached
```

---

## End-of-Game Scoring

Computed once at tick 576 (end of month 48). Not part of the tick loop.

### Neighborhood Score (60% of final)

```
inverseDebt = 100.0 - (debt * 100.0 / 80.0)

// Population normalization: population is absolute (not 0-100 like other metrics).
// POP_MAX_SCORE defines what population earns 100%. Set based on playtesting —
// whatever a well-played district realistically achieves by game end.
popScore = min((population / 1000.0) / POP_MAX_SCORE, 1.0) * 100.0

neighborhoodScore = GDP            * 0.225    // economic prosperity
                  + happiness      * 0.225    // resident satisfaction
                  + popScore       * 0.15     // district growth
                  + infrastructure * 0.15     // physical quality
                  + sustainability * 0.15     // long-term health
                  + inverseDebt    * 0.10     // fiscal responsibility
```

**NOTE — Double exposure for infrastructure neglect:** Low sustainability triggers
outmigration (hurting popScore) AND directly penalizes sustainability score. A player who
neglects infrastructure is penalized on BOTH components. This is intentional — the PDD
calls infrastructure neglect "the most punishing long-term failure mode."

### City Contribution Score (40% of final)

```
// City Reputation — how well the entire metro is doing (shared across all players)
// Worth 50% of city score = 20% of final score. Partially determined by opponents.
cityReputationScore = cityMetrics.cityReputation

// Shared Infrastructure Contribution — RELATIVE measure.
// What fraction of total city contribution spending came from this player?
totalAllCitySpending = sum(all districts' totalCitySpending)
if totalAllCitySpending > 0:
    sharedInfraContrib = (myTotalCitySpending / totalAllCitySpending) * 100.0
else:
    sharedInfraContrib = 100.0 / numActivePlayers    // no one spent anything, equal credit

// Crisis Avoidance — did this player avoid being a drag on the city?
// Penalized for ticks spent at debt cap or below happiness 20.
crisisTicks = ticksAtDebtCap + ticksBelowHappiness20
crisisAvoidance = max(0, 100 - crisisTicks * K_CRISIS_PENALTY)

cityContribScore = cityReputationScore * 0.50
                 + sharedInfraContrib  * 0.25
                 + crisisAvoidance     * 0.25
```

### Final Score

```
finalScore = neighborhoodScore * 0.60 + cityContribScore * 0.40
```

**RISK — POP_MAX_SCORE calibration:** If too low, everyone maxes population score easily
and it doesn't differentiate. If too high, population score is always low and feels
unrewarding. Run simulated games during calibration, use the 90th percentile final
population as POP_MAX_SCORE.

**RISK — Cross-session score incomparability:** City reputation (20% of final score) depends
on ALL players' metrics. A player in a strong game scores higher than the same player in a
weak game. Scores are only meaningful within the same session, not across sessions.

---

## Constants Reference

All tunable constants in one place. Values marked TBD require calibration during Sprint 1.
Fixed values come from the PDD or design decisions documented above.

### Budget Constants

```
K_REV               = 1.0         // revenue scaling (fixed — other constants tune relative to this)
K_SPEND             = 3.0         // spending scaling (derived from starting balance constraint)
K_CITY_WEIGHT       = 1.0         // city contribution cost multiplier (1.0 = same as domestic)
K_DEBT_ACCRUAL      = TBD         // how fast deficit → debt. Calibrate: ~40-50 ticks of max deficit to reach cap from starting debt 15.
K_DEBT_RECOVERY     = K_DEBT_ACCRUAL / 3.0    // 3:1 asymmetry: debt accrues 3x faster than it recovers
K_RESERVE_DECAY     = 0.005       // reserve loses 0.5% per tick (prevents indefinite hoarding)
DEBT_CAP            = 60          // spending scaling activates at this debt level
RESERVE_CAP         = 22500       // max reserve (~20 ticks of starting revenue)
```

### GDP Constants

```
K_EDU_TO_GDP        = TBD         // education spending → GDP growth (largest single GDP lever)
K_INFRA_TO_GDP      = TBD         // infrastructure metric level → GDP (secondary, ~60% of K_EDU_TO_GDP effect)
K_POP_TO_GDP        = TBD         // log(population) → GDP (tertiary)
K_SUSTAIN_TO_GDP    = TBD         // sustainability metric level → GDP (smallest, ~40% of K_EDU_TO_GDP effect)
K_TAX_GDP_DRAG      = TBD         // tax rate → GDP drag (proportional to current GDP)
K_ENV_GDP_DRAG      = TBD         // environment spending → GDP drag (mild)
K_GDP_DECAY         = TBD         // natural GDP decay rate per tick
```

### Happiness Constants

```
W_HAPPY_GDP         = 0.30        // GDP weight in metric baseline
W_HAPPY_INFRA       = 0.25        // infrastructure weight in metric baseline
W_HAPPY_SUSTAIN     = 0.25        // sustainability weight in metric baseline
W_HAPPY_DEBT        = 0.20        // inverse-debt weight in metric baseline
K_BASELINE_WEIGHT   = 0.60        // metric baseline contributes 60% of happiness
K_HOUSING_TO_HAPPY  = TBD         // housing spending → direct happiness boost (calibrate to 0-50 output range)
K_TAX_HAPPY_PENALTY = TBD         // tax rate → happiness penalty (calibrate to 0-30 output range at max tax)
K_DEBT_STRESS       = TBD         // debt above 40 → happiness drag (calibrate to meaningful but not overwhelming)
K_HAPPY_SMOOTHING   = 1.0         // 1.0 = no smoothing (instant). Lower if happiness is too volatile in testing.
```

### Infrastructure Constants

```
K_INFRA_TO_INFRA    = TBD         // infrastructure spending → infrastructure metric growth
K_INFRA_DECAY       = TBD         // natural decay rate. Sweet spot: maintaining level 50 needs moderate ongoing investment.
```

### Sustainability Constants

```
K_INFRA_TO_SUSTAIN  = TBD         // infrastructure metric level → sustainability (PRIMARY input, 3-4x K_ENV_TO_SUSTAIN)
K_ENV_TO_SUSTAIN    = TBD         // environment spending → sustainability (secondary input)
K_POP_SUSTAIN_DRAIN = TBD         // population → sustainability drain (larger districts = more pressure)
K_SUSTAIN_DECAY     = TBD         // natural entropy rate
SUSTAIN_MIGRATION_THRESHOLD = 30  // below this, residents start leaving
K_MIGRATION_RATE    = TBD         // how fast population leaves per point below threshold
MIN_POPULATION      = 1.0         // 1k minimum — prevents division by zero, district is functionally dead
MAX_POPULATION      = 1000.0      // 1M theoretical ceiling
```

### Spillover Constants

```
// Gentrification
GENTRIFY_THRESHOLD         = 8     // GDP gap required to trigger
K_GENTRIFY_HAPPY           = TBD   // happiness damage to poor district per magnitude point
K_GENTRIFY_POP             = TBD   // population loss from poor district per magnitude point
K_GENTRIFY_GDP_GAIN        = TBD   // GDP gain for wealthy district (economic expansion)
K_GENTRIFY_WEALTHY_HAPPY   = TBD   // happiness cost for wealthy district (congestion, inequality)

// Pollution
POLLUTE_ENV_THRESHOLD      = 30    // environment spending below this can cause pollution
POLLUTE_GDP_THRESHOLD      = 40    // GDP above this can cause pollution (need industrial activity)
K_POLLUTION_GENERATE       = TBD   // base pollution output scaling
K_POLLUTION_SUSTAIN        = TBD   // sustainability damage to neighbors per pollution unit
K_POLLUTION_HAPPY          = TBD   // happiness damage to neighbors per pollution unit
K_POLLUTION_SELF_SUSTAIN   = TBD   // sustainability self-damage (~0.5x K_POLLUTION_SUSTAIN)
K_POLLUTION_SELF_HAPPY     = TBD   // happiness self-damage (~0.5x K_POLLUTION_HAPPY)

// Commuting
COMMUTE_GDP_THRESHOLD      = 5     // GDP gap required to trigger
COMMUTE_INFRA_THRESHOLD    = 25    // shared infrastructure quality required to enable commuting
K_COMMUTE_VOLUME           = TBD   // how many commuters per magnitude point * infra factor
K_COMMUTE_GDP_GAIN         = TBD   // GDP gain for work district per commuter
K_COMMUTE_CONGESTION       = TBD   // happiness cost for work district per commuter
K_COMMUTE_GDP_DRAIN        = TBD   // GDP drain from home district per commuter
K_COMMUTE_HOME_HAPPY       = TBD   // happiness boost for home district (employed residents)
```

### City Metrics Constants

```
K_VARIANCE_PENALTY         = TBD   // how much inequality hurts city reputation
K_POP_INFLOW_HIGH          = TBD   // population inflow rate when reputation > 70
K_POP_INFLOW_NORMAL        = TBD   // population inflow rate when reputation 30-70
K_POP_OUTFLOW              = TBD   // population outflow rate when reputation < 30
K_SHARED_INFRA_GROWTH      = TBD   // collective city spending → shared infra growth
K_SHARED_INFRA_DECAY       = TBD   // shared infra natural decay rate
```

### Federal Funding Constants

```
GRANT_BASE_GREEN           = TBD   // base bonus revenue for green infrastructure grant
GRANT_BASE_TRANSIT         = TBD   // base bonus revenue for federal transit grant
GRANT_BASE_LIFE            = TBD   // base bonus revenue for quality of life grant
GRANT_BASE_DEV             = TBD   // base bonus revenue for development grant
K_STABILIZATION_RATE       = TBD   // debt reduction per tick when debt >= 70
```

### Scoring Constants

```
POP_MAX_SCORE              = TBD   // population (in thousands) that earns 100% population score
K_CRISIS_PENALTY           = TBD   // score penalty per crisis tick (debt cap or happiness < 20)
```

### Starting Values

```
GDP_START                  = 50
HAPPINESS_START            = 55
POPULATION_START           = 150.0     // 150k
INFRASTRUCTURE_START       = 50
SUSTAINABILITY_START       = 55
DEBT_START                 = 15
RESERVE_START              = 0
SHARED_INFRA_START         = 50
TAX_RATE_DEFAULT           = 15
SLIDERS_DEFAULT            = 50
CITY_CONTRIB_DEFAULT       = 25
```

---

## Implementation Notes

### File Structure (suggested)

Place all simulation code in `Assets/Scripts/Simulation/`:

```
Assets/Scripts/Simulation/
    SimulationConstants.cs       // All K_ values, thresholds, starting values
    GameState.cs                 // GameState, DistrictState, PolicySliders, CityMetrics structs
    TickProcessor.cs             // Main entry: ResolveTick(GameState) → GameState
    BudgetCalculator.cs          // Phase 1: revenue, spending, debt cap, reserve/debt
    LocalEffectCalculator.cs     // Phase 2: GDP, happiness, infrastructure, sustainability
    SpilloverResolver.cs         // Phase 3: gentrification, pollution, commuting
    CityMetricsManager.cs        // Phase 4: reputation, population, shared infra, grants
    ScoringSystem.cs             // End-of-game: neighborhood + city contribution scores
    AdjacencyMap.cs              // District neighbor relationships and weights
```

### Testing Strategy

Every formula should be unit-testable with known inputs and expected outputs. Because all
simulation logic is in static pure functions with no Unity dependencies, tests can run in
any C# test framework (NUnit, which Unity includes, is the natural choice).

Priority test cases:
1. Balanced budget at starting values (revenue = spending)
2. Debt cap scaling produces correct scaleFactor
3. Reserve absorbs deficit before debt accrues
4. Surplus pays debt before filling reserve
5. GDP diminishing returns approach 100 asymptotically
6. Sustainability outmigration triggers at threshold 30
7. Gentrification fires at GDP differential > 8
8. Pollution requires BOTH env < 30 AND gdp > 40
9. Commuting requires BOTH gdp differential > 5 AND shared infra > 25
10. City reputation variance penalty increases with inequality
11. Final scoring produces correct 60/40 split

### Calibration Approach

All TBD constants should be exposed as public fields in SimulationConstants.cs so they
can be adjusted at runtime during playtesting without recompiling. Use a ScriptableObject
or a simple static config class. The goal during Sprint 1:

1. Set initial K values based on "reasonable first guesses" (the formulas above give
   relative relationships — education should be ~40% of GDP growth, etc.)
2. Run a 576-tick simulation with all players at default sliders. Verify metrics stay
   near starting values (steady state at defaults).
3. Run a simulation with one player maxing education. Verify GDP grows at a reasonable
   pace (reaching ~75-80 by game end, not 95+).
4. Run a simulation with one player zeroing infrastructure. Verify sustainability declines,
   outmigration triggers around tick 100-150, and the district is clearly struggling by
   mid-game.
5. Iterate.
