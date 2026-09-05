using Logic.Grouping.Generation;
using Logic.Grouping.Scoring;
using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior specific to <see cref="SimulatedAnnealingWrapper"/>
/// (inner generation + Metropolis swap search), not the shared producer contract.
/// </summary>
public class SimulatedAnnealingWrapperTests
{
    #region Delegation and polishing

    [Fact]
    public void GenerateStream_WithoutEnumeration_DoesNotPullFromInner()
    {
        var inner = new CountingStubProducer(CreateSplitComposition());
        var wrapper = new SimulatedAnnealingWrapper(inner, CreateScorer(), SimulatedAnnealingConfig.Fast);

        _ = wrapper.GenerateStream();

        Assert.Equal(0, inner.ItemsYielded);
    }

    [Fact]
    public void GenerateStream_TakeThree_PullsThreeFromInner()
    {
        var inner = new CountingStubProducer(CreateSplitComposition());
        var wrapper = new SimulatedAnnealingWrapper(inner, CreateScorer(), SimulatedAnnealingConfig.Fast);

        _ = wrapper.GenerateStream().Take(3).ToList();

        Assert.Equal(3, inner.ItemsYielded);
    }

    [Fact]
    public void ImprovingSwap_IsKept()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        var baseline = new GroupComposition(
        [
            new Group([a, c]),
            new Group([b, d]),
        ]);

        var inner = new CountingStubProducer(baseline);
        var wrapper = new SimulatedAnnealingWrapper(inner, CreateScorer(points: 3), SingleStepConfig())
        {
            ProposeSwap = _ => (0, 1, 1, 0),
        };

        GroupComposition result = wrapper.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002"],
            result.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004"],
            result.Groups[1].Members.Select(student => student.Number));
        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void WorseningSwap_IsRejected_WhenAcceptanceDrawIsTooHigh()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        var baseline = new GroupComposition(
        [
            new Group([a, b]),
            new Group([c, d]),
        ]);

        var wrapper = new SimulatedAnnealingWrapper(CreateScorer(points: 3), SingleStepConfig())
        {
            ProposeSwap = _ => (0, 1, 1, 0),
            // Exp(negative / T) < 1, so drawing 1.0 always rejects.
            NextAcceptanceDraw = static () => 1.0,
        };

        GroupComposition polished = wrapper.Polish(baseline);

        Assert.Equal(
            ["100001", "100002"],
            polished.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004"],
            polished.Groups[1].Members.Select(student => student.Number));
        Assert.Equal(6, polished.TotalScore);
    }

    [Fact]
    public void WorseningSwap_CanBeAccepted_WhenAcceptanceDrawIsLowEnough()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        var baseline = new GroupComposition(
        [
            new Group([a, b]),
            new Group([c, d]),
        ]);

        int proposal = 0;
        var wrapper = new SimulatedAnnealingWrapper(CreateScorer(points: 3), SingleStepConfig())
        {
            ProposeSwap = _ =>
            {
                proposal++;
                // Only the first proposal worsens; later steps skip.
                return proposal == 1 ? (0, 1, 1, 0) : null;
            },
            NextAcceptanceDraw = static () => 0.0,
        };

        GroupComposition polished = wrapper.Polish(baseline);

        // Metropolis accepted the downhill move; with no further improving swaps,
        // the tracked best remains the original layout.
        Assert.Equal(
            ["100001", "100002"],
            polished.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004"],
            polished.Groups[1].Members.Select(student => student.Number));
        Assert.Equal(6, polished.TotalScore);
    }

    [Fact]
    public void ReturnsGlobalBest_EvenAfterCurrentLayoutWandersDownhill()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        var baseline = new GroupComposition(
        [
            new Group([a, b]),
            new Group([c, d]),
        ]);

        // Temperature steps: 10 > 0.1, then 5 > 0.1, then 2.5 > 0.1, then 1.25 > 0.1, then 0.625 > 0.1, then 0.3125 > 0.1, then stop.
        // One step per temperature → several proposals; accept first worsening, reject further.
        var config = new SimulatedAnnealingConfig(
            initialTemperature: 10,
            coolingRate: 0.5,
            minTemperature: 0.1,
            stepsPerTemp: 1);

        int proposal = 0;
        var wrapper = new SimulatedAnnealingWrapper(CreateScorer(points: 3), config)
        {
            ProposeSwap = _ =>
            {
                proposal++;
                return (0, 1, 1, 0);
            },
            NextAcceptanceDraw = () => proposal == 1 ? 0.0 : 1.0,
        };

        GroupComposition polished = wrapper.Polish(baseline);

        Assert.Equal(
            ["100001", "100002"],
            polished.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004"],
            polished.Groups[1].Members.Select(student => student.Number));
        Assert.Equal(6, polished.TotalScore);
    }

    [Fact]
    public void RunsConfiguredSwapProposals_AcrossTemperatureSchedule()
    {
        // T: 8 → 4 → 2 → 1 → 0.5 (stop when 0.5 is not > 0.5) → 4 temps × 3 steps = 12
        var config = new SimulatedAnnealingConfig(
            initialTemperature: 8,
            coolingRate: 0.5,
            minTemperature: 0.5,
            stepsPerTemp: 3);

        int proposals = 0;
        var wrapper = new SimulatedAnnealingWrapper(CreateScorer(), config)
        {
            ProposeSwap = groups =>
            {
                proposals++;
                return (0, 0, 1, 0);
            },
            NextAcceptanceDraw = static () => 1.0,
        };

        _ = wrapper.Polish(CreateSplitComposition());

        Assert.Equal(12, proposals);
    }

    [Fact]
    public void DefaultConstructor_UsesMutualPairFirstInner()
    {
        var students = CreateStudents(6);
        var wrapper = new SimulatedAnnealingWrapper(
            StudentList.Create(students),
            GroupSizeDistribution.Create([3, 3], 6),
            CreateScorer(),
            SimulatedAnnealingConfig.Fast);

        GroupComposition composition = wrapper.GenerateStream().First();

        Assert.Equal(2, composition.Groups.Count);
        Assert.Equal(0, composition.TotalScore);
        Assert.Equal(
            students.Select(s => s.Number).OrderBy(n => n).ToArray(),
            composition.Groups.SelectMany(g => g.Members).Select(s => s.Number).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void Constructor_NullInner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SimulatedAnnealingWrapper(null!, CreateScorer(), SimulatedAnnealingConfig.Fast));
    }

    [Fact]
    public void Constructor_NullScorer_Throws()
    {
        var inner = new CountingStubProducer(CreateSplitComposition());

        Assert.Throws<ArgumentNullException>(() =>
            new SimulatedAnnealingWrapper(inner, null!, SimulatedAnnealingConfig.Fast));
    }

    [Fact]
    public void ScorerOnlyConstructor_NullScorer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SimulatedAnnealingWrapper(null!, SimulatedAnnealingConfig.Fast));
    }

    [Fact]
    public void ScorerOnlyConstructor_GenerateStream_Throws()
    {
        var wrapper = new SimulatedAnnealingWrapper(CreateScorer(), SimulatedAnnealingConfig.Fast);

        Assert.Throws<InvalidOperationException>(() =>
            wrapper.GenerateStream().First());
    }

    #endregion

    #region Config validation

    [Fact]
    public void Config_InvalidInitialTemperature_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(initialTemperature: 0.01, minTemperature: 0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(initialTemperature: 0.005, minTemperature: 0.01));
    }

    [Fact]
    public void Config_InvalidCoolingRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(coolingRate: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(coolingRate: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(coolingRate: -0.1));
    }

    [Fact]
    public void Config_InvalidMinTemperature_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(minTemperature: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(minTemperature: -1));
    }

    [Fact]
    public void Config_InvalidStepsPerTemp_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(stepsPerTemp: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulatedAnnealingConfig(stepsPerTemp: -3));
    }

    [Fact]
    public void Config_ResolveStepsPerTemp_DefaultsToStudentCountTimesTen()
    {
        var config = new SimulatedAnnealingConfig();

        Assert.Equal(40, config.ResolveStepsPerTemp(4));
    }

    #endregion

    #region Public Polish

    [Fact]
    public void Polish_DoesNotMutateBaselineGroups()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        var baseline = new GroupComposition(
        [
            new Group([a, c]),
            new Group([b, d]),
        ]);
        var originalNumbers = baseline.Groups
            .Select(g => g.Members.Select(s => s.Number).ToArray())
            .ToArray();

        var wrapper = new SimulatedAnnealingWrapper(CreateScorer(points: 3), SingleStepConfig())
        {
            ProposeSwap = _ => (0, 1, 1, 0),
        };

        _ = wrapper.Polish(baseline);

        Assert.Equal(originalNumbers[0], baseline.Groups[0].Members.Select(s => s.Number));
        Assert.Equal(originalNumbers[1], baseline.Groups[1].Members.Select(s => s.Number));
    }

    [Fact]
    public void Polish_ReturnsNewInstance_WithImprovedPartitionAndScore()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        var baseline = new GroupComposition(
        [
            new Group([a, c]),
            new Group([b, d]),
        ]);

        var wrapper = new SimulatedAnnealingWrapper(CreateScorer(points: 3), SingleStepConfig())
        {
            ProposeSwap = _ => (0, 1, 1, 0),
        };

        GroupComposition polished = wrapper.Polish(baseline);

        Assert.NotSame(baseline, polished);
        Assert.Equal(
            ["100001", "100002"],
            polished.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004"],
            polished.Groups[1].Members.Select(student => student.Number));
        Assert.Equal(6, polished.TotalScore);
    }

    [Fact]
    public void Polish_Cancelled_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        var wrapper = new SimulatedAnnealingWrapper(CreateScorer(), SimulatedAnnealingConfig.Fast)
        {
            ProposeSwap = _ =>
            {
                cts.Cancel();
                return (0, 0, 1, 0);
            },
        };

        Assert.ThrowsAny<OperationCanceledException>(() =>
            wrapper.Polish(CreateSplitComposition(), cts.Token));
    }

    [Fact]
    public void GenerateStream_StillYieldsUnscoredCompositions()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        var baseline = new GroupComposition(
        [
            new Group([a, c]),
            new Group([b, d]),
        ]);

        var inner = new CountingStubProducer(baseline);
        var wrapper = new SimulatedAnnealingWrapper(inner, CreateScorer(points: 3), SingleStepConfig())
        {
            ProposeSwap = _ => (0, 1, 1, 0),
        };

        GroupComposition result = wrapper.GenerateStream().First();

        Assert.Equal(0, result.TotalScore);
        Assert.Equal(
            ["100001", "100002"],
            result.Groups[0].Members.Select(student => student.Number));
    }

    #endregion

    #region Helpers

    private static SimulatedAnnealingConfig SingleStepConfig() =>
        new(initialTemperature: 10, coolingRate: 0.1, minTemperature: 1, stepsPerTemp: 1);

    private static GroupCompositionScorer CreateScorer(double points = 3) =>
        new([new MutualMatch(points)]);

    private static GroupComposition CreateSplitComposition()
    {
        var students = CreateStudents(4);
        return new GroupComposition(
        [
            new Group(students.Take(2).ToList()),
            new Group(students.Skip(2).Take(2).ToList()),
        ]);
    }

    private static List<Student> CreateStudents(int count)
        => Enumerable.Range(0, count)
            .Select(index => new Student((100001 + index).ToString(), []))
            .ToList();

    private sealed class CountingStubProducer(GroupComposition template) : IGroupCompositionProducer
    {
        public int ItemsYielded { get; private set; }

        public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ItemsYielded++;
                yield return template;
            }
        }
    }

    #endregion
}
