using Logic.Grouping.Generation;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior that is specific to <see cref="IslandFirstStrategy"/>
/// (wish-graph connected-component seeding, then BFS fill), not the shared producer contract.
/// </summary>
public class IslandFirstStrategyTests
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

    #region Island seeding

    [Fact]
    public void SeedsGroupWithConnectedIsland_WhenItFits()
    {
        // Island {A,B,C} via A→B→C (undirected). Size 3 → seed whole island.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100004", "100005", "100006"],
            composition.Groups[1].Members.Select(student => student.Number));
    }

    [Fact]
    public void PrefersLargerIsland_WhenMultipleFit()
    {
        // Islands: {A,B,C} size 3 and {D,E} size 2. First group size 3 → take the larger island.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", ["100005"]);
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
        // Remaining {D,E} size 2 fits in size 3, then fill with F.
        Assert.Equal(
            ["100004", "100005", "100006"],
            composition.Groups[1].Members.Select(student => student.Number));
    }

    [Fact]
    public void IslandLargerThanTarget_IsSkipped_FallsBackToSingleSeed()
    {
        // One island of 6 — never fits size 3 → PopFirst A then BFS along the chain.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100005"]);
        Student e = Student.Create("100005", ["100006"]);
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void OneWayWish_StillConnectsIsland()
    {
        // Only A→B (not mutual); still one undirected island {A,B}.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", Array.Empty<string>());
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d };

        var strategy = CreateStrategy(students, [2, 2], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002"],
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100004"],
            composition.Groups[1].Members.Select(student => student.Number));
    }

    [Fact]
    public void NoEdges_FallsBackToShuffledOrderSeeding()
    {
        var students = CreateStudents(6);
        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100004", "100005", "100006"],
            composition.Groups[1].Members.Select(student => student.Number));
    }

    #endregion

    #region BFS fill after island seed

    [Fact]
    public void AfterPartialIslandSeed_FillsRemainingWithBfs()
    {
        // Island {A,B} size 2; group size 3 → seed island then fall back PopFirst C.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", Array.Empty<string>());
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void InjectedWishPicker_ControlsWhoJoinsAfterIslandSeed()
    {
        // One oversized island (size 6) → no island seed; PopFirst A then PickWish among wishes.
        Student a = Student.Create("100001", ["100002", "100003", "100004", "100005", "100006"]);
        Student b = Student.Create("100002", Array.Empty<string>());
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var pickC = CreateStrategy(
            students,
            [3, 3],
            IdentityShuffle,
            PreferNumberThenFirst("100003"));
        var pickD = CreateStrategy(
            students,
            [3, 3],
            IdentityShuffle,
            PreferNumberThenFirst("100004"));

        GroupComposition withC = pickC.GenerateStream().First();
        GroupComposition withD = pickD.GenerateStream().First();

        Assert.Equal("100001", withC.Groups[0].Members[0].Number);
        Assert.Equal("100003", withC.Groups[0].Members[1].Number);

        Assert.Equal("100001", withD.Groups[0].Members[0].Number);
        Assert.Equal("100004", withD.Groups[0].Members[1].Number);
    }

    #endregion

    #region Helpers

    private static IslandFirstStrategy CreateStrategy(
        IReadOnlyList<Student> students,
        IReadOnlyList<int> groupSizes,
        Func<IReadOnlyList<Student>, IReadOnlyList<Student>>? shuffle = null,
        Func<IReadOnlyList<Student>, Student>? pickWish = null)
    {
        var strategy = new IslandFirstStrategy(
            StudentList.Create(students.ToList()),
            GroupSizeDistribution.Create(groupSizes, students.Count));
        if (shuffle is not null)
        {
            strategy.Shuffle = shuffle;
        }

        if (pickWish is not null)
        {
            strategy.PickWish = pickWish;
        }

        return strategy;
    }

    private static List<Student> CreateStudents(int count)
        => Enumerable.Range(0, count)
            .Select(index => new Student(FormatStudentNumber(index), []))
            .ToList();

    private static string FormatStudentNumber(int index) => (100001 + index).ToString();

    private static IReadOnlyList<Student> IdentityShuffle(IReadOnlyList<Student> students) => students;

    private static Student PickFirst(IReadOnlyList<Student> candidates) => candidates[0];

    private static Func<IReadOnlyList<Student>, Student> PreferNumberThenFirst(string preferredNumber)
        => candidates => candidates.FirstOrDefault(student => student.Number == preferredNumber)
                         ?? candidates[0];

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
