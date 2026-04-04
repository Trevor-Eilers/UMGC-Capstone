// Author: Malcolm Bramble

using System;
using System.Text;
using System.IO;
using NUnit.Framework;

/// <summary>
/// Simulation test harness for running full-game scenarios with configurable
/// strategy profiles. Outputs CSV data for calibration analysis.
/// Pure C# — no MonoBehaviour dependencies.
/// </summary>
[TestFixture]
public class SimulationHarness
{
    // ──────────────────────────────────────────────
    // Strategy Profiles
    // ──────────────────────────────────────────────

    public static void ApplyProfile(ref DistrictState d, string profile)
    {
        switch (profile)
        {
            case "balanced":
                d.sliders.taxRate = 15f;
                d.sliders.education = 50f;
                d.sliders.infrastructure = 50f;
                d.sliders.housing = 50f;
                d.sliders.environment = 50f;
                d.sliders.cityContribution = 25f;
                break;

            case "education_heavy":
                d.sliders.taxRate = 15f;
                d.sliders.education = 80f;
                d.sliders.infrastructure = 40f;
                d.sliders.housing = 40f;
                d.sliders.environment = 40f;
                d.sliders.cityContribution = 25f;
                break;

            case "infra_neglect":
                d.sliders.taxRate = 15f;
                d.sliders.education = 70f;
                d.sliders.infrastructure = 10f;
                d.sliders.housing = 60f;
                d.sliders.environment = 50f;
                d.sliders.cityContribution = 25f;
                break;

            case "high_tax_saver":
                d.sliders.taxRate = 25f;
                d.sliders.education = 30f;
                d.sliders.infrastructure = 30f;
                d.sliders.housing = 30f;
                d.sliders.environment = 30f;
                d.sliders.cityContribution = 25f;
                break;

            case "free_rider":
                d.sliders.taxRate = 15f;
                d.sliders.education = 60f;
                d.sliders.infrastructure = 60f;
                d.sliders.housing = 60f;
                d.sliders.environment = 60f;
                d.sliders.cityContribution = 0f;
                break;

            default:
                throw new ArgumentException($"Unknown profile: {profile}");
        }
    }

    // ──────────────────────────────────────────────
    // Calibrated Constants
    // ──────────────────────────────────────────────

    private static void SetCalibratedConstants()
    {
        // Budget
        SimulationConstants.K_REV = 1.0f;
        SimulationConstants.K_SPEND = 3.0f;
        SimulationConstants.K_CITY_WEIGHT = 1.0f;
        SimulationConstants.K_DEBT_ACCRUAL = 0.005f;
        SimulationConstants.K_DEBT_RECOVERY = SimulationConstants.K_DEBT_ACCRUAL / 3.0f;
        SimulationConstants.K_RESERVE_DECAY = 0.005f;
        SimulationConstants.DEBT_CAP = 60f;
        SimulationConstants.RESERVE_CAP = 22500f;

        // GDP
        SimulationConstants.K_EDU_TO_GDP = 0.0012f;
        SimulationConstants.K_INFRA_TO_GDP = 0.001f;
        SimulationConstants.K_POP_TO_GDP = 0.03f;
        SimulationConstants.K_SUSTAIN_TO_GDP = 0.001f;
        SimulationConstants.K_TAX_GDP_DRAG = 0.005f;
        SimulationConstants.K_ENV_GDP_DRAG = 0.0003f;
        SimulationConstants.K_GDP_DECAY = 0.006f;

        // Happiness
        SimulationConstants.W_HAPPY_GDP = 0.30f;
        SimulationConstants.W_HAPPY_INFRA = 0.25f;
        SimulationConstants.W_HAPPY_SUSTAIN = 0.25f;
        SimulationConstants.W_HAPPY_DEBT = 0.20f;
        SimulationConstants.K_BASELINE_WEIGHT = 0.60f;
        SimulationConstants.K_HOUSING_TO_HAPPY = 0.08f;
        SimulationConstants.K_TAX_HAPPY_PENALTY = 5.0f;
        SimulationConstants.K_DEBT_STRESS = 0.3f;
        SimulationConstants.K_HAPPY_SMOOTHING = 1.0f;

        // Infrastructure
        SimulationConstants.K_INFRA_TO_INFRA = 0.002f;
        SimulationConstants.K_INFRA_DECAY = 0.0045f;

        // Sustainability
        SimulationConstants.K_INFRA_TO_SUSTAIN = 0.002f;
        SimulationConstants.K_ENV_TO_SUSTAIN = 0.001f;
        SimulationConstants.K_POP_SUSTAIN_DRAIN = 0.001f;
        SimulationConstants.K_SUSTAIN_DECAY = 0.0014f;
        SimulationConstants.SUSTAIN_MIGRATION_THRESHOLD = 30f;
        SimulationConstants.K_MIGRATION_RATE = 0.5f;
        SimulationConstants.MIN_POPULATION = 1.0f;
        SimulationConstants.MAX_POPULATION = 1000.0f;

        // Spillover
        SimulationConstants.GENTRIFY_THRESHOLD = 8f;
        SimulationConstants.K_GENTRIFY_HAPPY = 0.5f;
        SimulationConstants.K_GENTRIFY_POP = 0.1f;
        SimulationConstants.K_GENTRIFY_GDP_GAIN = 0.3f;
        SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY = 0.2f;
        SimulationConstants.POLLUTE_ENV_THRESHOLD = 30f;
        SimulationConstants.POLLUTE_GDP_THRESHOLD = 40f;
        SimulationConstants.K_POLLUTION_GENERATE = 0.1f;
        SimulationConstants.K_POLLUTION_SUSTAIN = 0.1f;
        SimulationConstants.K_POLLUTION_HAPPY = 0.05f;
        SimulationConstants.K_POLLUTION_SELF_SUSTAIN = 0.05f;
        SimulationConstants.K_POLLUTION_SELF_HAPPY = 0.025f;
        SimulationConstants.COMMUTE_GDP_THRESHOLD = 5f;
        SimulationConstants.COMMUTE_INFRA_THRESHOLD = 25f;
        SimulationConstants.K_COMMUTE_VOLUME = 0.5f;
        SimulationConstants.K_COMMUTE_GDP_GAIN = 0.3f;
        SimulationConstants.K_COMMUTE_CONGESTION = 0.1f;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = 0.2f;
        SimulationConstants.K_COMMUTE_HOME_HAPPY = 0.1f;

        // City Metrics
        SimulationConstants.K_VARIANCE_PENALTY = 1.0f;
        SimulationConstants.K_POP_INFLOW_HIGH = 0.2f;
        SimulationConstants.K_POP_INFLOW_NORMAL = 0.02f;
        SimulationConstants.K_POP_OUTFLOW = 0.5f;
        SimulationConstants.K_SHARED_INFRA_GROWTH = 0.0002f;
        SimulationConstants.K_SHARED_INFRA_DECAY = 0.005f;

        // Federal Funding
        SimulationConstants.GRANT_BASE_GREEN = 50f;
        SimulationConstants.GRANT_BASE_TRANSIT = 50f;
        SimulationConstants.GRANT_BASE_LIFE = 50f;
        SimulationConstants.GRANT_BASE_DEV = 50f;
        SimulationConstants.K_STABILIZATION_RATE = 1.0f;

        // Scoring
        SimulationConstants.POP_MAX_SCORE = 300f;
        SimulationConstants.K_CRISIS_PENALTY = 0.5f;
    }

    // ──────────────────────────────────────────────
    // Core Runner
    // ──────────────────────────────────────────────

    /// <summary>
    /// Runs a full 576-tick simulation with the given strategy profiles.
    /// Returns CSV string with monthly snapshots and final scores.
    /// </summary>
    public static string RunScenario(
        string scenarioName,
        string[] profiles,
        int switchTick = -1,
        string switchProfile = null,
        int switchPlayer = -1)
    {
        SetCalibratedConstants();

        GameState state = GameState.NewGame(4);

        // Apply initial profiles
        for (int i = 0; i < 4; i++)
            ApplyProfile(ref state.districts[i], profiles[i]);

        var csv = new StringBuilder();

        // Header
        csv.AppendLine("tick,district,gdp,happiness,population,infrastructure," +
                       "sustainability,debt,reserve,revenue,totalSpending");

        // Run simulation
        for (int tick = 0; tick < SimulationConstants.TOTAL_TICKS; tick++)
        {
            // Mid-game strategy switch
            if (tick == switchTick && switchPlayer >= 0 && switchProfile != null)
                ApplyProfile(ref state.districts[switchPlayer], switchProfile);

            state = TickProcessor.ResolveTick(state);

            // Report every 12 ticks (once per simulated month)
            if (state.currentTick % SimulationConstants.TICKS_PER_MONTH == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    var d = state.districts[i];
                    csv.AppendLine(string.Format(
                        "{0},{1},{2:F2},{3:F2},{4:F2},{5:F2},{6:F2},{7:F2},{8:F2},{9:F2},{10:F2}",
                        state.currentTick, i,
                        d.gdp, d.happiness, d.population,
                        d.infrastructure, d.sustainability,
                        d.debt, d.reserve, d.revenue, d.totalSpending));
                }
            }
        }

        // Final scores
        csv.AppendLine();
        csv.AppendLine("# Final Scores");
        csv.AppendLine("district,profile,neighborhoodScore,cityContribScore,finalScore");

        for (int i = 0; i < 4; i++)
        {
            FinalScore score = ScoringSystem.ComputeFinalScore(
                state.districts[i], state.cityMetrics, state.districts, 4);
            csv.AppendLine(string.Format("{0},{1},{2:F2},{3:F2},{4:F2}",
                i, profiles[i],
                score.neighborhoodScore, score.cityContribScore, score.finalScore));
        }

        return csv.ToString();
    }

    private static void WriteScenario(string filename, string csv)
    {
        string outputPath = Path.GetFullPath(
            Path.Combine(UnityEngine.Application.dataPath,
                "Scripts", "Simulation", "Tests", filename));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, csv);
        UnityEngine.Debug.Log($"Wrote {outputPath} ({csv.Split('\n').Length} lines)");
    }

    // ──────────────────────────────────────────────
    // Scenarios
    // ──────────────────────────────────────────────

    [Test]
    public void ScenarioA_AllBalanced()
    {
        string csv = RunScenario("A_AllBalanced",
            new[] { "balanced", "balanced", "balanced", "balanced" });
        WriteScenario("ScenarioA_AllBalanced.csv", csv);
        Assert.Pass("Scenario A complete — see CSV output");
    }

    [Test]
    public void ScenarioB_EducationHeavy()
    {
        string csv = RunScenario("B_EducationHeavy",
            new[] { "education_heavy", "balanced", "balanced", "balanced" });
        WriteScenario("ScenarioB_EducationHeavy.csv", csv);
        Assert.Pass("Scenario B complete — see CSV output");
    }

    [Test]
    public void ScenarioC_InfraNeglect()
    {
        string csv = RunScenario("C_InfraNeglect",
            new[] { "infra_neglect", "balanced", "balanced", "balanced" });
        WriteScenario("ScenarioC_InfraNeglect.csv", csv);
        Assert.Pass("Scenario C complete — see CSV output");
    }

    [Test]
    public void ScenarioD_HighTaxSaverSwitch()
    {
        // Player 0 saves with high taxes for 200 ticks, then switches to education_heavy
        string csv = RunScenario("D_HighTaxSaverSwitch",
            new[] { "high_tax_saver", "balanced", "balanced", "balanced" },
            switchTick: 200,
            switchProfile: "education_heavy",
            switchPlayer: 0);
        WriteScenario("ScenarioD_HighTaxSaverSwitch.csv", csv);
        Assert.Pass("Scenario D complete — see CSV output");
    }

    [Test]
    public void ScenarioE_FreeRider()
    {
        string csv = RunScenario("E_FreeRider",
            new[] { "free_rider", "balanced", "balanced", "balanced" });
        WriteScenario("ScenarioE_FreeRider.csv", csv);
        Assert.Pass("Scenario E complete — see CSV output");
    }
}
