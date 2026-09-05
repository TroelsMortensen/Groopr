using Logic.Grouping.Generation;
using Logic.Grouping.Scoring;
using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior specific to <see cref="EdgeContractionMatchingStrategy"/>
/// (affinity-ordered edge contraction, then blueprint rebalancing), not the shared producer contract.
/// </summary>
public class EdgeContractionMatchingStrategyTests
{
    #region Shuffle once per composition

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

    #region Edge contraction preferences

    [Fact]
    public void ContractsHighestAffinityMutualPair_IntoSameGroup()
    {
        // A↔B mutual is the strongest dyad; size-2 blueprint should keep them together.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d };

        var strategy = CreateStrategy(students, [2, 2], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Contains(
            composition.Groups,
            group => group.Members.Select(m => m.Number).OrderBy(n => n)
                .SequenceEqual(["100001", "100002"]));
    }

    [Fact]
    public void PrefersMutualPairOverOneWayWish()
    {
        Student a = Student.Create("100001", ["100003"]);
        Student b = Student.Create("100002", ["100004"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", ["100002"]);
        var students = new List<Student> { a, b, c, d };

        var strategy = CreateStrategy(students, [2, 2], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Contains(
            composition.Groups,
            group => group.Members.Select(m => m.Number).OrderBy(n => n)
                .SequenceEqual(["100002", "100004"]));
    }

    [Fact]
    public void ContractsMultipleMutualPairs_IntoSeparateGroups()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [2, 2, 2], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Contains(
            composition.Groups,
            group => group.Members.Select(m => m.Number).OrderBy(n => n)
                .SequenceEqual(["100001", "100002"]));
        Assert.Contains(
            composition.Groups,
            group => group.Members.Select(m => m.Number).OrderBy(n => n)
                .SequenceEqual(["100003", "100004"]));
    }

    [Fact]
    public void AvoidsContractingNegativeWishPair_WhenBetterEdgeExists()
    {
        Student a = Student.Create(
            "100001",
            [StudentNumber.Create("100002")],
            negativeWishes: [StudentNumber.Create("100003")]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d };

        var strategy = CreateStrategy(students, [2, 2], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Contains(
            composition.Groups,
            group => group.Members.Select(m => m.Number).OrderBy(n => n)
                .SequenceEqual(["100001", "100002"]));
        Assert.DoesNotContain(
            composition.Groups,
            group =>
            {
                var numbers = group.Members.Select(m => m.Number).ToHashSet();
                return numbers.Contains("100001") && numbers.Contains("100003");
            });
    }

    [Fact]
    public void ExpandsContractedPair_WithHighestAffinityNeighbor()
    {
        // A↔B mutual contracts first. Both wish C, so C should join them for size 3.
        Student a = Student.Create("100001", ["100002", "100003"]);
        Student b = Student.Create("100002", ["100001", "100003"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Contains(
            composition.Groups,
            group => group.Members.Select(m => m.Number).OrderBy(n => n)
                .SequenceEqual(["100001", "100002", "100003"]));
    }

    #endregion

    #region Capacity and blueprint

    [Fact]
    public void DoesNotExceedMaxBlueprintSize_DuringContraction()
    {
        // Chain of mutual wishes that could greedily grow past max size 3 without a cap.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001", "100003"]);
        Student c = Student.Create("100003", ["100002", "100004"]);
        Student d = Student.Create("100004", ["100003", "100005"]);
        Student e = Student.Create("100005", ["100004", "100006"]);
        Student f = Student.Create("100006", ["100005"]);
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle);

        foreach (GroupComposition composition in strategy.GenerateStream().Take(20))
        {
            Assert.All(composition.Groups, group => Assert.True(group.Members.Count <= 3));
            Assert.Equal([3, 3], composition.Groups.Select(g => g.Members.Count).ToArray());
        }
    }

    [Fact]
    public void NoPreferenceStructure_StillMatchesBlueprint()
    {
        var students = CreateStudents(11);
        var strategy = CreateStrategy(students, [4, 4, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal([4, 4, 3], composition.Groups.Select(g => g.Members.Count).ToArray());
        Assert.Equal(
            students.Select(s => s.Number).OrderBy(n => n),
            composition.Groups.SelectMany(g => g.Members).Select(s => s.Number).OrderBy(n => n));
    }

    [Fact]
    public void TargetSizeOne_YieldsSingletonGroups()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        var students = new List<Student> { a, b, c };

        var strategy = CreateStrategy(students, [1, 1, 1], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.All(composition.Groups, group => Assert.Single(group.Members));
        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups.SelectMany(g => g.Members).Select(s => s.Number).OrderBy(n => n));
    }

    #endregion

    #region Helpers

    private static EdgeContractionMatchingStrategy CreateStrategy(
        IReadOnlyList<Student> students,
        IReadOnlyList<int> groupSizes,
        Func<IReadOnlyList<Student>, IReadOnlyList<Student>>? shuffle = null)
    {
        var strategy = new EdgeContractionMatchingStrategy(
            StudentList.Create(students.ToList()),
            GroupSizeDistribution.Create(groupSizes, students.Count),
            CreateDefaultScorer());
        if (shuffle is not null)
        {
            strategy.Shuffle = shuffle;
        }

        return strategy;
    }

    private static GroupCompositionScorer CreateDefaultScorer() =>
        new(
        [
            new MutualMatch(3),
            new PartialMatch(1),
            new NegativeMatch(3),
        ]);

    private static List<Student> CreateStudents(int count)
        => Enumerable.Range(0, count)
            .Select(index => new Student(FormatStudentNumber(index), []))
            .ToList();

    private static string FormatStudentNumber(int index) => (100001 + index).ToString();

    private static IReadOnlyList<Student> IdentityShuffle(IReadOnlyList<Student> students) => students;

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

    #endregion
}
