using Logic.Grouping.Generation;
using Logic.Grouping.Scoring;
using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior specific to <see cref="MatrixWindowScanStrategy"/>
/// (score-sorted dyad anchors, then greedy marginal expansion), not the shared producer contract.
/// </summary>
public class MatrixWindowScanStrategyTests
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

    #region Dyad anchoring

    [Fact]
    public void SeedsGroupWithHighestScoringDyad_WhenAvailable()
    {
        // A↔B mutual (highest pair score). Size 2 → that dyad becomes the first group.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d };

        var strategy = CreateStrategy(students, [2, 2], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002"],
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004"],
            composition.Groups[1].Members.Select(student => student.Number));
    }

    [Fact]
    public void PrefersMutualDyadOverOneWayWish()
    {
        // A→C one-way; B↔D mutual. First size-2 group should be the mutual pair.
        Student a = Student.Create("100001", ["100003"]);
        Student b = Student.Create("100002", ["100004"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", ["100002"]);
        var students = new List<Student> { a, b, c, d };

        var strategy = CreateStrategy(students, [2, 2], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100002", "100004"],
            composition.Groups[0].Members.Select(student => student.Number).OrderBy(n => n));
    }

    [Fact]
    public void UsesSeparateDyads_ForSuccessiveGroups()
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

        Assert.Equal(
            ["100001", "100002"],
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004"],
            composition.Groups[1].Members.Select(student => student.Number));
        Assert.Equal(
            ["100005", "100006"],
            composition.Groups[2].Members.Select(student => student.Number));
    }

    [Fact]
    public void NoScoredStructure_FallsBackToSequentialTake()
    {
        var students = CreateStudents(6);
        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        // All dyad scores are 0; first pair in shuffled (identity) order anchors, then fills.
        Assert.Equal(3, composition.Groups[0].Members.Count);
        Assert.Equal(3, composition.Groups[1].Members.Count);
        Assert.Equal(
            students.Select(s => s.Number).OrderBy(n => n),
            composition.Groups.SelectMany(g => g.Members).Select(s => s.Number).OrderBy(n => n));
    }

    [Fact]
    public void TargetSizeOne_DoesNotSeedWithDyad()
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
            composition.Groups.SelectMany(group => group.Members).Select(student => student.Number));
    }

    #endregion

    #region Window expansion

    [Fact]
    public void AfterDyadAnchor_ExpandsByHighestMarginalScore()
    {
        // A↔B mutual anchors. C is wished by both (partials from A and B); D is isolated.
        // Size 3 → expand with C over D.
        Student a = Student.Create("100001", ["100002", "100003"]);
        Student b = Student.Create("100002", ["100001", "100003"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void ExpansionPrefersCandidateThatCompletesMutualMatch()
    {
        // A↔B mutual anchors. A↔C would also be mutual if C joins; D only has one-way from A.
        Student a = Student.Create("100001", ["100002", "100003", "100004"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100001"]);
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void AvoidsExpandingWithNegativeWishWhenBetterCandidateExists()
    {
        // A↔B mutual. C has no relation (score 0). D is blacklisted by A (negative).
        Student a = Student.Create(
            "100001",
            [StudentNumber.Create("100002")],
            negativeWishes: [StudentNumber.Create("100004")]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.DoesNotContain(
            composition.Groups[0].Members,
            member => member.Number == "100004");
    }

    [Fact]
    public void WhenNoAnchorRemains_TakesNextUnassignedSequentially()
    {
        // Only one mutual pair; after it is consumed for group 1, group 2 falls back to Take.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [2, 4], IdentityShuffle);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002"],
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004", "100005", "100006"],
            composition.Groups[1].Members.Select(student => student.Number));
    }

    #endregion

    #region Helpers

    private static MatrixWindowScanStrategy CreateStrategy(
        IReadOnlyList<Student> students,
        IReadOnlyList<int> groupSizes,
        Func<IReadOnlyList<Student>, IReadOnlyList<Student>>? shuffle = null)
    {
        var strategy = new MatrixWindowScanStrategy(
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
