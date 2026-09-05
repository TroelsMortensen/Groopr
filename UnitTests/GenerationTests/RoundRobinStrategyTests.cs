using Logic.Grouping.Generation;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior that is specific to <see cref="RoundRobinStrategy"/>
/// (phased delegation to child producers), not the shared producer contract.
/// </summary>
public class RoundRobinStrategyTests
{
    #region Phase switching

    [Fact]
    public void GenerateStream_WithoutEnumeration_DoesNotPullFromChildren()
    {
        var first = new CountingStubProducer(CreateFixedComposition(), tag: 1);
        var second = new CountingStubProducer(CreateFixedComposition(), tag: 2);
        var strategy = new RoundRobinStrategy([first, second], phaseLength: 3);

        _ = strategy.GenerateStream();

        Assert.Equal(0, first.ItemsYielded);
        Assert.Equal(0, second.ItemsYielded);
        Assert.Equal(0, first.StreamStarts);
        Assert.Equal(0, second.StreamStarts);
    }

    [Fact]
    public void GenerateStream_AlternatesProducersInPhaseBlocks()
    {
        var first = new CountingStubProducer(CreateFixedComposition(), tag: 1);
        var second = new CountingStubProducer(CreateFixedComposition(), tag: 2);
        var strategy = new RoundRobinStrategy([first, second], phaseLength: 3);

        List<double> tags = strategy.GenerateStream()
            .Take(7)
            .Select(composition => composition.TotalScore)
            .ToList();

        Assert.Equal([1, 1, 1, 2, 2, 2, 1], tags);
        Assert.Equal(4, first.ItemsYielded);
        Assert.Equal(3, second.ItemsYielded);
    }

    [Fact]
    public void GenerateStream_WithThreeProducers_CyclesThroughAll()
    {
        var a = new CountingStubProducer(CreateFixedComposition(), tag: 1);
        var b = new CountingStubProducer(CreateFixedComposition(), tag: 2);
        var c = new CountingStubProducer(CreateFixedComposition(), tag: 3);
        var strategy = new RoundRobinStrategy([a, b, c], phaseLength: 2);

        List<double> tags = strategy.GenerateStream()
            .Take(7)
            .Select(composition => composition.TotalScore)
            .ToList();

        Assert.Equal([1, 1, 2, 2, 3, 3, 1], tags);
    }

    [Fact]
    public void GenerateStream_PhaseLengthOne_SwitchesEveryComposition()
    {
        var first = new CountingStubProducer(CreateFixedComposition(), tag: 1);
        var second = new CountingStubProducer(CreateFixedComposition(), tag: 2);
        var strategy = new RoundRobinStrategy([first, second], phaseLength: 1);

        List<double> tags = strategy.GenerateStream()
            .Take(4)
            .Select(composition => composition.TotalScore)
            .ToList();

        Assert.Equal([1, 2, 1, 2], tags);
    }

    #endregion

    #region Cancellation and validation

    [Fact]
    public void GenerateStream_CancelledMidPhase_StopsWithoutFinishingPhase()
    {
        var first = new CountingStubProducer(CreateFixedComposition(), tag: 1);
        var second = new CountingStubProducer(CreateFixedComposition(), tag: 2);
        var strategy = new RoundRobinStrategy([first, second], phaseLength: 10);
        using var cts = new CancellationTokenSource();

        int count = 0;
        foreach (GroupComposition _ in strategy.GenerateStream(cts.Token))
        {
            count++;
            if (count == 3)
            {
                cts.Cancel();
            }
        }

        Assert.Equal(3, count);
        Assert.Equal(3, first.ItemsYielded);
        Assert.Equal(0, second.ItemsYielded);
    }

    [Fact]
    public void Constructor_NullProducers_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RoundRobinStrategy(null!, phaseLength: 10));
    }

    [Fact]
    public void Constructor_EmptyProducers_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new RoundRobinStrategy(Array.Empty<IGroupCompositionProducer>(), phaseLength: 10));
    }

    [Fact]
    public void Constructor_NonPositivePhaseLength_Throws()
    {
        var stub = new CountingStubProducer(CreateFixedComposition(), tag: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RoundRobinStrategy([stub], phaseLength: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RoundRobinStrategy([stub], phaseLength: -1));
    }

    [Fact]
    public void DefaultConstructor_YieldsStructurallyValidCompositions()
    {
        var students = CreateStudents(6);
        var strategy = new RoundRobinStrategy(
            StudentList.Create(students),
            GroupSizeDistribution.Create([3, 3], 6),
            phaseLength: 5);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(2, composition.Groups.Count);
        Assert.Equal(3, composition.Groups[0].Members.Count);
        Assert.Equal(3, composition.Groups[1].Members.Count);
        Assert.Equal(
            students.Select(student => student.Number).OrderBy(n => n).ToArray(),
            composition.Groups.SelectMany(group => group.Members).Select(s => s.Number).OrderBy(n => n).ToArray());
    }

    #endregion

    #region Helpers

    private static GroupComposition CreateFixedComposition()
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

    private sealed class CountingStubProducer(GroupComposition template, double tag) : IGroupCompositionProducer
    {
        public int ItemsYielded { get; private set; }

        public int StreamStarts { get; private set; }

        public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
        {
            StreamStarts++;
            while (!cancellationToken.IsCancellationRequested)
            {
                ItemsYielded++;
                yield return new GroupComposition(template.Groups, tag);
            }
        }
    }

    #endregion
}
