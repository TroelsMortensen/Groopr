using Logic.Grouping.Generation;
using Logic.Grouping.Scoring;
using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior that is specific to <see cref="HillClimbingWrapper"/>
/// (inner generation + swap-based local search), not the shared producer contract.
/// </summary>
public class HillClimbingWrapperTests
{
    #region Delegation and polishing

    [Fact]
    public void GenerateStream_WithoutEnumeration_DoesNotPullFromInner()
    {
        var inner = new CountingStubProducer(CreateSplitComposition());
        var wrapper = new HillClimbingWrapper(inner, CreateScorer(), iterations: 10);

        _ = wrapper.GenerateStream();

        Assert.Equal(0, inner.ItemsYielded);
    }

    [Fact]
    public void GenerateStream_TakeThree_PullsThreeFromInner()
    {
        var inner = new CountingStubProducer(CreateSplitComposition());
        var wrapper = new HillClimbingWrapper(inner, CreateScorer(), iterations: 5);

        _ = wrapper.GenerateStream().Take(3).ToList();

        Assert.Equal(3, inner.ItemsYielded);
    }

    [Fact]
    public void ImprovingSwap_IsKept()
    {
        // Baseline: mutual pairs split across groups → score 0.
        // One swap of B and C yields A↔B and C↔D together.
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
        // Swap group0[1]=C with group1[0]=B
        var wrapper = new HillClimbingWrapper(inner, CreateScorer(points: 3), iterations: 1)
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
    public void WorseningSwap_IsReverted()
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

        var inner = new CountingStubProducer(baseline);
        // Swap that breaks both mutuals
        var wrapper = new HillClimbingWrapper(inner, CreateScorer(points: 3), iterations: 1)
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
    }

    [Fact]
    public void RunsConfiguredIterationCount_OfSwapProposals()
    {
        var inner = new CountingStubProducer(CreateSplitComposition());
        int proposals = 0;
        var wrapper = new HillClimbingWrapper(inner, CreateScorer(), iterations: 17)
        {
            ProposeSwap = groups =>
            {
                proposals++;
                return (0, 0, 1, 0);
            },
        };

        _ = wrapper.GenerateStream().First();

        Assert.Equal(17, proposals);
    }

    [Fact]
    public void DefaultConstructor_UsesMutualPairFirstInner()
    {
        var students = CreateStudents(6);
        var wrapper = new HillClimbingWrapper(
            StudentList.Create(students),
            GroupSizeDistribution.Create([3, 3], 6),
            CreateScorer(),
            iterations: 5);

        GroupComposition composition = wrapper.GenerateStream().First();

        Assert.Equal(2, composition.Groups.Count);
        Assert.Equal(0, composition.TotalScore);
        Assert.Equal(
            students.Select(s => s.Number).OrderBy(n => n).ToArray(),
            composition.Groups.SelectMany(g => g.Members).Select(s => s.Number).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void Constructor_NonPositiveIterations_Throws()
    {
        var inner = new CountingStubProducer(CreateSplitComposition());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HillClimbingWrapper(inner, CreateScorer(), iterations: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HillClimbingWrapper(inner, CreateScorer(), iterations: -1));
    }

    [Fact]
    public void Constructor_NullInner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HillClimbingWrapper(null!, CreateScorer(), iterations: 10));
    }

    [Fact]
    public void Constructor_NullScorer_Throws()
    {
        var inner = new CountingStubProducer(CreateSplitComposition());

        Assert.Throws<ArgumentNullException>(() =>
            new HillClimbingWrapper(inner, null!, iterations: 10));
    }

    #endregion

    #region Helpers

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
