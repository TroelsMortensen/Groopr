using System.Diagnostics;
using Logic.GroupSizing;
using Logic.Grouping.Generation;
using Logic.Models;

namespace UnitTests;

public class GroupCompositionProducerTests
{
    #region Lazy execution / infinite stream

    [Fact]
    public void GenerateStream_WithoutEnumeration_DoesNotInvokeShuffler()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);

        _ = strategy.GenerateStream();

        Assert.Equal(0, countingShuffle.InvocationCount);
    }

    [Fact]
    public void GenerateStream_TakeFive_YieldsExactlyFiveCompositionsAndInvokesShufflerFiveTimes()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);

        List<GroupComposition> compositions = strategy.GenerateStream().Take(5).ToList();

        Assert.Equal(5, compositions.Count);
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
    public void GenerateStream_TakeOneThousand_CompletesWithinReasonableTime()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], IdentityShuffle);
        Stopwatch stopwatch = Stopwatch.StartNew();

        int count = strategy.GenerateStream().Take(1000).Count();

        stopwatch.Stop();
        Assert.Equal(1000, count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Taking 1000 compositions took {stopwatch.Elapsed}.");
    }

    [Fact]
    public void GenerateStream_CanBeEnumeratedIndependentlyTwice()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);
        IEnumerable<GroupComposition> stream = strategy.GenerateStream();

        int firstPassCount = stream.Take(3).Count();
        int secondPassCount = stream.Take(3).Count();

        Assert.Equal(3, firstPassCount);
        Assert.Equal(3, secondPassCount);
        Assert.Equal(6, countingShuffle.InvocationCount);
    }

    #endregion

    #region Correct group sizing

    [Theory]
    [InlineData(new[] { 4, 4, 3 })]
    [InlineData(new[] { 5, 5, 5 })]
    [InlineData(new[] { 3 })]
    [InlineData(new[] { 1, 1, 1 })]
    [InlineData(new[] { 2, 2, 2, 2, 2 })]
    [InlineData(new[] { 11, 12, 10 })]
    public void GeneratedCompositions_MatchGroupSizeBlueprint(int[] groupSizes)
    {
        int studentCount = groupSizes.Sum();
        var strategy = CreateStrategy(CreateStudents(studentCount), groupSizes, IdentityShuffle);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(25))
        {
            Assert.Equal(groupSizes.Length, composition.Groups.Count);
            Assert.Equal(groupSizes, composition.Groups.Select(group => group.Members.Count).ToArray());
        }
    }

    [Fact]
    public void SingleGroupBlueprint_PutsEveryStudentInOneGroup()
    {
        var students = CreateStudents(7);
        var strategy = CreateStrategy(students, [7], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Single(composition.Groups);
        Assert.Equal(7, composition.Groups[0].Members.Count);
    }

    #endregion

    #region All students included

    [Fact]
    public void GeneratedComposition_ContainsEveryStudentExactlyOnce()
    {
        var students = CreateStudents(11);
        var strategy = CreateStrategy(students, [4, 4, 3], IdentityShuffle);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(25))
        {
            string[] expected = students.Select(student => student.Number).OrderBy(number => number).ToArray();
            string[] actual = Flatten(composition).OrderBy(number => number).ToArray();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void GeneratedComposition_DoesNotDuplicateStudentsAcrossGroups()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], IdentityShuffle);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(25))
        {
            Assert.Equal(11, Flatten(composition).Distinct().Count());
        }
    }

    [Fact]
    public void GeneratedComposition_UsesSameStudentInstances()
    {
        var students = CreateStudents(11);
        var strategy = CreateStrategy(students, [4, 4, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();
        HashSet<Student> producedStudents = composition.Groups
            .SelectMany(group => group.Members)
            .ToHashSet();

        Assert.Equal(students.Count, producedStudents.Count);
        Assert.All(students, student => Assert.Contains(student, producedStudents));
    }

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

    [Fact]
    public void GenerateStream_DoesNotModifyInputLists()
    {
        List<Student> students = CreateStudents(11);
        int[] groupSizes = [4, 4, 3];
        var strategy = CreateStrategy(students, groupSizes, IdentityShuffle);

        _ = strategy.GenerateStream().Take(50).ToList();

        Assert.Equal(ExpectedStudentNumbers(0, 11), students.Select(student => student.Number));
        Assert.Equal(new[] { 4, 4, 3 }, groupSizes);
    }

    [Fact]
    public void GeneratedCompositions_AreDistinctObjects()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], IdentityShuffle);

        List<GroupComposition> compositions = strategy.GenerateStream().Take(2).ToList();

        Assert.NotSame(compositions[0], compositions[1]);
        Assert.NotSame(compositions[0].Groups, compositions[1].Groups);
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

    #endregion

    #region Cancellation support

    [Fact]
    public void PreCancelledToken_YieldsEmptySequenceWithoutThrowing()
    {
        var countingShuffle = new CountingShuffle(IdentityShuffle);
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], countingShuffle.Invoke);
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        List<GroupComposition> compositions = [];
        Exception? exception = Record.Exception(() =>
            compositions = strategy.GenerateStream(cancellationTokenSource.Token).ToList());

        Assert.Null(exception);
        Assert.Empty(compositions);
        Assert.Equal(0, countingShuffle.InvocationCount);
    }

    [Fact]
    public void CancellationDuringEnumeration_StopsAfterRequestedItems()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], IdentityShuffle);
        using CancellationTokenSource cancellationTokenSource = new();

        int count = 0;
        foreach (GroupComposition _ in strategy.GenerateStream(cancellationTokenSource.Token))
        {
            count++;
            if (count == 5)
            {
                cancellationTokenSource.Cancel();
            }
        }

        Assert.Equal(5, count);
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

    [Fact]
    public void GenerateStream_WithNoneToken_ProducesItems()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], IdentityShuffle);

        Assert.NotEmpty(strategy.GenerateStream(CancellationToken.None).Take(1));
    }

    [Fact]
    public void GenerateStream_WithDefaultToken_ProducesItems()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], IdentityShuffle);

        Assert.NotEmpty(strategy.GenerateStream().Take(1));
    }

    #endregion

    #region Invalid input

    [Fact]
    public void GroupSizesSumTooSmall_ThrowsOnConstruction()
    {
        Assert.ThrowsAny<Exception>(() =>
            new RandomShuffleStrategy(StudentList.Create(CreateStudents(11)), GroupSizeDistribution.Create([4, 4], 11)));
    }

    [Fact]
    public void GroupSizesSumTooLarge_ThrowsOnConstruction()
    {
        Assert.ThrowsAny<Exception>(() =>
            new RandomShuffleStrategy(
                StudentList.Create(CreateStudents(10)),
                GroupSizeDistribution.Create([4, 4, 3], 10)));
    }

    [Fact]
    public void EmptyStudentList_ThrowsOnConstruction()
    {
        Assert.ThrowsAny<Exception>(() =>
            StudentList.Create([]));
    }

    [Fact]
    public void EmptyGroupSizes_ThrowsOnConstruction()
    {
        Assert.ThrowsAny<Exception>(() =>
            GroupSizeDistribution.Create([], 11));
    }

    [Fact]
    public void NullStudents_ThrowsOnConstruction()
    {
        Assert.ThrowsAny<Exception>(() =>
            StudentList.Create(null!));
    }

    [Fact]
    public void NullGroupSizes_ThrowsOnConstruction()
    {
        Assert.ThrowsAny<Exception>(() =>
            GroupSizeDistribution.Create(null!, 11));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveGroupSize_ThrowsOnConstruction(int invalidGroupSize)
    {
        Assert.ThrowsAny<Exception>(() =>
            GroupSizeDistribution.Create([4, invalidGroupSize, 3], 11));
    }

    [Fact]
    public void ValidInput_DoesNotThrowOnConstruction()
    {
        Exception? exception = Record.Exception(() =>
            new RandomShuffleStrategy(
                StudentList.Create(CreateStudents(11)),
                GroupSizeDistribution.Create([4, 4, 3], 11)));

        Assert.Null(exception);
    }

    #endregion

    #region Structural validity (SRS)

    [Theory]
    [InlineData(new[] { 4, 4, 3 })]
    [InlineData(new[] { 5, 5, 5 })]
    [InlineData(new[] { 3 })]
    [InlineData(new[] { 1, 1, 1 })]
    [InlineData(new[] { 2, 2, 2, 2, 2 })]
    [InlineData(new[] { 11, 12, 10 })]
    public void GeneratedCompositions_AreStructurallyValid(int[] groupSizes)
    {
        var students = CreateStudents(groupSizes.Sum());
        var strategy = CreateStrategy(students, groupSizes, IdentityShuffle);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(25))
        {
            AssertStructurallyValid(composition, students, groupSizes);
        }
    }

    [Fact]
    public void DefaultShuffler_ProducesStructurallyValidCompositions()
    {
        int[] groupSizes = [4, 4, 3];
        var students = CreateStudents(11);
        var strategy = CreateStrategy(students, groupSizes);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(50))
        {
            AssertStructurallyValid(composition, students, groupSizes);
        }
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
    public void GeneratedComposition_HasZeroTotalScore()
    {
        var strategy = CreateStrategy(CreateStudents(11), [4, 4, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(0, composition.TotalScore);
    }

    #endregion

    #region Full sweep

    [Theory]
    [MemberData(nameof(ValidStudentCountsWithPrioritySets))]
    public void ValidClassSizes_ProduceCorrectPartitions(int numberOfStudents, int[] priorities)
    {
        GroupSizeDistribution determineGroupSizeDistribution = GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities));
        int[] groupSizes = determineGroupSizeDistribution.Sizes.ToArray();
        var students = CreateStudents(numberOfStudents);
        var strategy = CreateStrategy(students, groupSizes, IdentityShuffle);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(20))
        {
            AssertStructurallyValid(composition, students, groupSizes);
        }
    }

    public static TheoryData<int, int[]> ValidStudentCountsWithPrioritySets()
    {
        int[][] prioritySets =
        [
            [4, 5, 3],
            [5, 4, 3],
            [3, 4, 5],
        ];

        TheoryData<int, int[]> data = new();
        foreach (int[] priorities in prioritySets)
        {
            for (int numberOfStudents = 3; numberOfStudents <= 45; numberOfStudents++)
            {
                if (CanBeDivided(numberOfStudents, priorities))
                {
                    data.Add(numberOfStudents, priorities);
                }
            }
        }

        return data;
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

    /// <summary>
    /// Asserts all SRS structural rules for a valid GroupComposition.
    /// </summary>
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

    private static bool CanBeDivided(int numberOfStudents, int[] priorities)
    {
        if (numberOfStudents < priorities.Min())
        {
            return false;
        }

        return CanBeSummedFrom(numberOfStudents, priorities);
    }

    private static bool CanBeSummedFrom(int total, int[] sizes)
    {
        if (total == 0)
        {
            return true;
        }

        if (total < 0)
        {
            return false;
        }

        bool[] reachable = new bool[total + 1];
        reachable[0] = true;

        for (int amount = 1; amount <= total; amount++)
        {
            reachable[amount] = sizes.Any(size => size > 0 && size <= amount && reachable[amount - size]);
        }

        return reachable[total];
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
