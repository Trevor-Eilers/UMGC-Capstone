// Author: Malcolm Bramble

[System.Serializable]
public struct PolicyValues
{
    public float taxRate;          // 5-30
    public float education;        // 0-100
    public float infrastructure;   // 0-100
    public float housing;          // 0-100
    public float environment;      // 0-100
    public float cityContribution; // 0-50

    public static PolicyValues Default()
    {
        return new PolicyValues
        {
            taxRate = SimulationConstants.TAX_RATE_DEFAULT,
            education = SimulationConstants.SLIDERS_DEFAULT,
            infrastructure = SimulationConstants.SLIDERS_DEFAULT,
            housing = SimulationConstants.SLIDERS_DEFAULT,
            environment = SimulationConstants.SLIDERS_DEFAULT,
            cityContribution = SimulationConstants.CITY_CONTRIB_DEFAULT
        };
    }
}
