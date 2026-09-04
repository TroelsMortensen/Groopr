using Logic.Grouping.Generation;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior that is specific to <see cref="RandomShuffleStrategy"/>
/// (injectable shuffle hook), not the shared producer contract.
/// </summary>
public class RandomShuffleStrategyTests
{
    #region Lazy execution / shuffle invocation

    [Fact]
    public void GenerateStream_WithoutEnumeration_DoesNotInvokeShuffler()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);

        _ = strategy.GenerateStream();

        Assert.Equal(0, countingShuffle.InvocationCount);
    }

    [Fact]
    public void GenerateStream_TakeFive_InvokesShufflerFiveTimes()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);

        _ = strategy.GenerateStream().Take(5).ToList();

        Assert.Equal(5, countingShuffle.InvocationCount);
    }

    [Fact]
    public void GenerateStream_BreakAfterThreeItems_InvokesShufflerThreeTimes()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);

        int count = 0;
        foreach (GroupComposition _ in strategy.GenerateStream())
        {
            count++;
            if (count == 3)
            {
                break;
            }
        }

        Assert.Equal(3, count);
        Assert.Equal(3, countingShuffle.InvocationCount);
    }

    [Fact]
    public void GenerateStream_CanBeEnumeratedIndependentlyTwice_InvokesShufflerPerPass()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);
        IEnumerable<GroupComposition> stream = strategy.GenerateStream();

        _ = stream.Take(3).Count();
        _ = stream.Take(3).Count();

        Assert.Equal(6, countingShuffle.InvocationCount);
    }

    #endregion

    #region Deterministic partitioning via shuffle

    [Fact]
    public void IdentityShuffler_PartitionsStudentsInInputOrder()
    {
        var students = CreateStudents(11);
        var strategy = CreateStrategy(students, [4, 4, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ExpectedStudentNumbers(0, 4),
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ExpectedStudentNumbers(4, 4),
            composition.Groups[1].Members.Select(student => student.Number));
        Assert.Equal(
            ExpectedStudentNumbers(8, 3),
            composition.Groups[2].Members.Select(student => student.Number));
    }

    #endregion

    #region Randomization / variance

    [Fact]
    public void RotatingShuffler_ProducesDifferentSuccessiveCompositions()
    {
        var students = CreateStudents(11);
        var rotatingShuffle = new RotatingShuffle();
        var strategy = CreateStrategy(students, [4, 4, 3], rotatingShuffle.Invoke);

        List<GroupComposition> compositions = strategy.GenerateStream().Take(3).ToList();
        string[] first = Flatten(compositions[0]).ToArray();
        string[] second = Flatten(compositions[1]).ToArray();
        string[] third = Flatten(compositions[2]).ToArray();

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
        Assert.NotEqual(first, third);
    }

    [Fact]
    public void DefaultShuffler_ProducesMoreThanOneDistinctArrangement()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3]);

        HashSet<string> distinctArrangements = strategy.GenerateStream()
            .Take(200)
            .Select(composition => string.Join("|", Flatten(composition)))
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(distinctArrangements.Count > 1,
            "Expected the default shuffler to produce more than one distinct arrangement.");
    }

    [Fact]
    public void IdentityShuffler_AllowsDuplicateCompositions()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], IdentityShuffle);

        List<string> arrangements = strategy.GenerateStream()
            .Take(10)
            .Select(composition => string.Join("|", Flatten(composition)))
            .ToList();

        Assert.Equal(10, arrangements.Count);
        Assert.True(arrangements.Distinct(StringComparer.Ordinal).Count() == 1);
    }

    [Fact]
    public void Shuffler_IsInvokedOncePerCompositionWithFullStudentPool()
    {
        var students = CreateStudents(11);
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(students, [4, 4, 3], countingShuffle.Invoke);

        _ = strategy.GenerateStream().Take(7).ToList();

        Assert.Equal(7, countingShuffle.InvocationCount);
        Assert.All(countingShuffle.ReceivedStudentLists, receivedStudents =>
        {
            Assert.Equal(students.Count, receivedStudents.Count);
            Assert.Equal(students, receivedStudents);
        });
    }

    [Fact]
    public void RotatingShuffler_ProducesStructurallyValidCompositions()
    {
        int[] groupSizes = [4, 4, 3];
        var students = CreateStudents(11);
        var strategy = CreateStrategy(students, groupSizes, new RotatingShuffle().Invoke);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(25))
        {
            AssertStructurallyValid(composition, students, groupSizes);
        }
    }

    [Fact]
    public void IdentityShuffler_ProducesStructurallyValidCompositions()
    {
        int[] groupSizes = [4, 4, 3];
        var students = CreateStudents(11);
        var strategy = CreateStrategy(students, groupSizes, IdentityShuffle);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(25))
        {
            AssertStructurallyValid(composition, students, groupSizes);
        }
    }

    #endregion

    #region Cancellation via shuffle

    [Fact]
    public void PreCancelledToken_DoesNotInvokeShuffler()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        _ = strategy.GenerateStream(cancellationTokenSource.Token).ToList();

        Assert.Equal(0, countingShuffle.InvocationCount);
    }

    [Fact]
    public void ShufflerTriggeredCancellation_TerminatesStreamCleanly()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        var cancellingShuffle = new CancellingShuffle(cancellationTokenSource, cancelOnInvocation: 1);
        var countingShuffle = new CountingShuffle(cancellingShuffle.Invoke);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);

        Exception? exception = Record.Exception(() =>
            strategy.GenerateStream(cancellationTokenSource.Token).Take(10).ToList());

        Assert.Null(exception);
        Assert.Equal(1, countingShuffle.InvocationCount);
    }

    #endregion

    #region Helpers and test doubles

    private static RandomShuffleStrategy CreateStrategy(
        IReadOnlyList<Student> students,
        IReadOnlyList<int> groupSizes,
        Func<IReadOnlyList<Student>, IReadOnlyList<Student>>? shuffle = null)
    {
        var strategy = new RandomShuffleStrategy(
            StudentList.Create(students.ToList()),
            GroupSizeDistribution.Create(groupSizes, students.Count));
        if (shuffle is not null)
        {
            strategy.Shuffle = shuffle;
        }

        return strategy;
    }

    private static List<Student> CreateStudents(int count)
        => Enumerable.Range(0, count)
            .Select(index => new Student(FormatStudentNumber(index), []))
            .ToList();

    private static string FormatStudentNumber(int index) => (100001 + index).ToString();

    private static string[] ExpectedStudentNumbers(int startIndex, int count)
        => Enumerable.Range(startIndex, count).Select(FormatStudentNumber).ToArray();

    private static IReadOnlyList<Student> IdentityShuffle(IReadOnlyList<Student> students) => students;

    private static IEnumerable<string> Flatten(GroupComposition composition)
        => composition.Groups.SelectMany(group => group.Members.Select(student => student.Number));

    private static void AssertStructurallyValid(
        GroupComposition composition,
        IReadOnlyList<Student> students,
        IReadOnlyList<int> groupSizes)
    {
        Assert.Equal(groupSizes.Sum(), students.Count);
        Assert.Equal(groupSizes.Count, composition.Groups.Count);
        Assert.Equal(groupSizes, composition.Groups.Select(group => group.Members.Count).ToArray());

        List<Student> members = composition.Groups.SelectMany(group => group.Members).ToList();
        Assert.Equal(students.Count, members.Count);
        Assert.Equal(students.Count, members.Distinct().Count());
        Assert.Equal(students.Count, members.Select(student => student.Number).Distinct().Count());

        HashSet<Student> pool = students.ToHashSet();
        Assert.All(members, member => Assert.Contains(member, pool));

        string[] expectedNumbers = students.Select(student => student.Number).OrderBy(number => number).ToArray();
        string[] actualNumbers = members.Select(student => student.Number).OrderBy(number => number).ToArray();
        Assert.Equal(expectedNumbers, actualNumbers);

        Assert.Equal(0, composition.TotalScore);
    }

    private sealed class RotatingShuffle
    {
        private int _offset;

        public IReadOnlyList<Student> Invoke(IReadOnlyList<Student> students)
        {
            _offset = (_offset + 1) % students.Count;
            return students.Skip(_offset).Concat(students.Take(_offset)).ToList();
        }
    }

    private sealed class CountingShuffle(Func<IReadOnlyList<Student>, IReadOnlyList<Student>> inner)
    {
        public int InvocationCount { get; private set; }

        public List<IReadOnlyList<Student>> ReceivedStudentLists { get; } = [];

        public IReadOnlyList<Student> Invoke(IReadOnlyList<Student> students)
        {
            InvocationCount++;
            ReceivedStudentLists.Add(students);
            return inner(students);
        }
    }

    private sealed class CancellingShuffle(CancellationTokenSource cancellationTokenSource, int cancelOnInvocation)
    {
        private int _invocationCount;

        public IReadOnlyList<Student> Invoke(IReadOnlyList<Student> students)
        {
            _invocationCount++;
            if (_invocationCount >= cancelOnInvocation)
            {
                cancellationTokenSource.Cancel();
            }

            return students;
        }
    }

    #endregion
}
