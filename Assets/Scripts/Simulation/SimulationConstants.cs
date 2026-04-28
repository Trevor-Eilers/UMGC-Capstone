// Author: Malcolm Bramble

public static class SimulationConstants
{
    // ── Budget Constants ──

    public static float K_REV = 1.0f;
    public static float K_SPEND = 3.0f;
    public static float GDP_MAINTAIN_THRESHOLD = 50f;
    public static float K_GDP_MAINTAIN = 3.0f;
    public static float K_CITY_WEIGHT = 1.0f;
    public static float K_DEBT_ACCRUAL = 0.04f;
    // Fraction of surplus that goes to debt service (vs. reserve fill).
    // 0.33 = one-third pays debt, two-thirds fills reserve. Combined with K_DEBT_ACCRUAL
    // this preserves the design's roughly-3:1 debt-accrues-faster-than-it-recovers
    // asymmetry while leaving reserve fillable during recovery ticks.
    public static float K_DEBT_RECOVERY = 0.33f;
    public static float K_RESERVE_DECAY = 0.005f;
    public static float DEBT_CAP = 60f;
    public static float RESERVE_CAP = 22500f;
    public static float K_DEBT_CAP_MIN_SCALE = 0.20f;

    // ── GDP Constants ──

    public static float K_EDU_TO_GDP = 0.002f;
    public static float K_INFRA_TO_GDP = 0.02f;
    public static float K_POP_TO_GDP = 0.10f;
    public static float K_SUSTAIN_TO_GDP = 0.008f;
    public static float K_TAX_GDP_DRAG = 0.06f;
    public static float K_ENV_GDP_DRAG = 0.0015f;
    public static float K_GDP_DECAY = 0.008f;

    // ── Happiness Constants ──

    public static float W_HAPPY_GDP = 0.30f;
    public static float W_HAPPY_INFRA = 0.25f;
    public static float W_HAPPY_SUSTAIN = 0.25f;
    public static float W_HAPPY_DEBT = 0.20f;
    public static float K_BASELINE_WEIGHT = 0.60f;
    public static float K_HOUSING_TO_HAPPY = 3.5f;
    public static float K_TAX_HAPPY_PENALTY = 15.0f;
    public static float K_DEBT_STRESS = 0.5f;
    public static float K_HAPPY_SMOOTHING = 1.0f;

    // ── Infrastructure Constants ──

    public static float K_INFRA_TO_INFRA = 0.005f;
    public static float K_INFRA_DECAY = 0.005f;

    // ── Sustainability Constants ──

    public static float K_INFRA_TO_SUSTAIN = 0.01f;
    public static float K_ENV_TO_SUSTAIN = 0.003f;
    public static float K_POP_SUSTAIN_DRAIN = 0.006f;
    public static float K_SUSTAIN_DECAY = 0.010f;
    // SUSTAIN_MIGRATION_THRESHOLD and K_MIGRATION_RATE are retained for
    // back-compat with existing test save/restore blocks; the migration path
    // now uses the carrying-capacity formula below.
    public static float SUSTAIN_MIGRATION_THRESHOLD = 30f;
    public static float K_MIGRATION_RATE = 0.5f;
    public static float MIN_POPULATION = 5.0f;
    public static float MAX_POPULATION = 1000.0f;

    // ── Carrying-Capacity Migration ──
    // Population units are thousands. K(d) = base + slider+state contributions.
    // Outmigration scales with overshoot above K, plus a sustain-collapse floor.

    public static float K_BASE = 5.0f;
    public static float K_HOUSING_CAP = 4.0f;
    public static float K_INFRA_CAP = 1.5f;
    public static float K_ENV_CAP = 1.5f;
    public static float K_SHARED_CAP = 1.5f;
    public static float K_REPUTATION_CAP = 1.0f;
    public static float K_OVERSHOOT_LINEAR = 0.05f;
    public static float K_OVERSHOOT_QUAD = 0.0015f;
    public static float SUSTAIN_COLLAPSE_THRESHOLD = 15f;
    public static float K_SUSTAIN_COLLAPSE_RATE = 0.5f;
    public static float HAPPINESS_COLLAPSE_THRESHOLD = 30f;
    public static float K_HAPPINESS_COLLAPSE_RATE = 0.3f;

    // ── Spillover: Gentrification ──

    public static float GENTRIFY_THRESHOLD = 12f;
    public static float K_GENTRIFY_HAPPY = 0.3f;
    public static float K_GENTRIFY_POP = 0.1f;
    public static float K_GENTRIFY_GDP_GAIN = 0.1f;
    public static float K_GENTRIFY_WEALTHY_HAPPY = 0.1f;

    // ── Spillover: Pollution ──

    public static float POLLUTE_ENV_THRESHOLD = 30f;
    public static float POLLUTE_GDP_THRESHOLD = 40f;
    public static float K_POLLUTION_GENERATE = 0.05f;
    public static float K_POLLUTION_SUSTAIN = 0.1f;
    public static float K_POLLUTION_HAPPY = 0.05f;
    public static float K_POLLUTION_SELF_SUSTAIN = 0.05f;
    public static float K_POLLUTION_SELF_HAPPY = 0.025f;
    public static float POLLUTE_GDP_HIGH_THRESHOLD = 75f;
    public static float K_POLLUTE_ENV_OFFSET = 0.5f;

    // ── Spillover: Commuting ──

    public static float COMMUTE_GDP_THRESHOLD = 5f;
    public static float COMMUTE_INFRA_THRESHOLD = 25f;
    public static float K_COMMUTE_VOLUME = 0.1f;
    public static float K_COMMUTE_GDP_GAIN = 0.1f;
    public static float K_COMMUTE_CONGESTION = 0.05f;
    public static float K_COMMUTE_GDP_DRAIN = 0.05f;
    public static float K_COMMUTE_HOME_HAPPY = 0.03f;

    // ── City Metrics Constants ──

    public static float K_VARIANCE_PENALTY = 0.5f;
    public static float K_POP_INFLOW_HIGH = 0.2f;
    public static float K_POP_INFLOW_NORMAL = 0.06f;
    public static float K_POP_OUTFLOW = 0.3f;
    public static float K_SHARED_INFRA_GROWTH = 0.00145f;
    public static float K_SHARED_INFRA_DECAY = 0.02f;

    // ── Federal Funding Constants ──

    public static float GRANT_BASE_GREEN = 20.0f;
    public static float GRANT_BASE_TRANSIT = 20.0f;
    public static float GRANT_BASE_LIFE = 20.0f;
    public static float GRANT_BASE_DEV = 20.0f;
    public static float GRANT_GREEN_THRESHOLD = 65f;
    public static float GRANT_TRANSIT_THRESHOLD = 250f;
    public static float GRANT_LIFE_THRESHOLD = 70f;
    public static float GRANT_DEV_THRESHOLD = 70f;
    public static float K_STABILIZATION_RATE = 2.5f;
    public static float REPUTATION_PENALTY_THRESHOLD = 40f;
    public static float K_REPUTATION_PENALTY_FLOOR = 0.65f;

    // ── Scoring Constants ──

    public static float POP_MAX_SCORE = 400.0f;
    public static float K_CRISIS_PENALTY = 1.5f;

    // ── Starting Values ──

    public const float GDP_START = 50f;
    public const float HAPPINESS_START = 55f;
    public const float POPULATION_START = 150.0f;
    public const float INFRASTRUCTURE_START = 50f;
    public const float SUSTAINABILITY_START = 55f;
    public const float DEBT_START = 15f;
    public const float RESERVE_START = 0f;
    public const float SHARED_INFRA_START = 50f;
    public const float TAX_RATE_DEFAULT = 15f;
    public const float SLIDERS_DEFAULT = 50f;
    public const float CITY_CONTRIB_DEFAULT = 25f;

    // ── Tick Timing ──

    public const int TICKS_PER_MONTH = 12;
    public const int TOTAL_MONTHS = 48;
    public const int TOTAL_TICKS = 576;
}