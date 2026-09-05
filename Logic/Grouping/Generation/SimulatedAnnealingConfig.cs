namespace Logic.Grouping.Generation;

/// <summary>
/// Annealing schedule for <see cref="SimulatedAnnealingWrapper"/>.
/// </summary>
public sealed class SimulatedAnnealingConfig
{
    public const double DefaultInitialTemperature = 100.0;
    public const double DefaultCoolingRate = 0.995;
    public const double DefaultMinTemperature = 0.01;
    public const int DefaultStepsPerTempMultiplier = 10;

    public double InitialTemperature { get; }
    public double CoolingRate { get; }
    public double MinTemperature { get; }

    /// <summary>
    /// Swap proposals evaluated at each temperature. When null, polish uses
    /// <c>studentCount * <see cref="DefaultStepsPerTempMultiplier"/></c>.
    /// </summary>
    public int? StepsPerTemp { get; }

    public SimulatedAnnealingConfig(
        double initialTemperature = DefaultInitialTemperature,
        double coolingRate = DefaultCoolingRate,
        double minTemperature = DefaultMinTemperature,
        int? stepsPerTemp = null)
    {
        if (minTemperature <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minTemperature),
                minTemperature,
                "MinTemperature must be greater than 0.");
        }

        if (initialTemperature <= minTemperature)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialTemperature),
                initialTemperature,
                "InitialTemperature must be greater than MinTemperature.");
        }

        if (coolingRate is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(coolingRate),
                coolingRate,
                "CoolingRate must be in the open interval (0, 1).");
        }

        if (stepsPerTemp is < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepsPerTemp),
                stepsPerTemp,
                "StepsPerTemp must be at least 1 when specified.");
        }

        InitialTemperature = initialTemperature;
        CoolingRate = coolingRate;
        MinTemperature = minTemperature;
        StepsPerTemp = stepsPerTemp;
    }

    public static SimulatedAnnealingConfig Default { get; } = new();

    /// <summary>
    /// Fast schedule for contract tests and light benchmarks.
    /// </summary>
    public static SimulatedAnnealingConfig Fast { get; } = new(
        initialTemperature: 10,
        coolingRate: 0.5,
        minTemperature: 0.1,
        stepsPerTemp: 5);

    public int ResolveStepsPerTemp(int studentCount)
    {
        if (studentCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(studentCount), studentCount, "Student count must be at least 1.");
        }

        return StepsPerTemp ?? studentCount * DefaultStepsPerTempMultiplier;
    }
}
