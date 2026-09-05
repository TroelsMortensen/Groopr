using Logic.Grouping.Generation;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior that is specific to <see cref="TriadFirstStrategy"/>
/// (directed-triangle seeding, mutual-pair/random fallback, BFS fill), not the shared producer contract.
/// </summary>
public class TriadFirstStrategyTests
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

    #region Triangle seeding

    [Fact]
    public void SeedsGroupWithDirectedTriangle_WhenAvailable()
    {
        // A→B→C→A cycle. Group size 3 → seed the triad exactly.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", ["100001"]);
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
    public void UsesSeparateTriangles_ForSuccessiveGroups()
    {
        // A→B→C→A and D→E→F→D
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", ["100001"]);
        Student d = Student.Create("100004", ["100005"]);
        Student e = Student.Create("100005", ["100006"]);
        Student f = Student.Create("100006", ["100004"]);
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
    public void IncompleteWishChain_IsNotTreatedAsTriangle()
    {
        // A→B→C but C does not wish A — not a directed cycle.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        // Falls back to single seed A, then BFS A→B→C.
        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void NoTriangle_FallsBackToMutualPairSeed()
    {
        // No directed triangle; A↔B mutual. Size 3 → seed A,B then fill C.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
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
    public void TargetSizeTwo_DoesNotSeedWithTriangle_UsesMutualPair()
    {
        // Triangle A→B→C→A exists, but size 2 → mutual A↔D seeds instead... 
        // Use triangle + separate mutual for clarity.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", ["100001"]);
        Student d = Student.Create("100004", ["100005"]);
        Student e = Student.Create("100005", ["100004"]);
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [2, 2, 2], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        // No triad seeding; first mutual in scan order is none among A,B,C (one-way cycle edges aren't mutual).
        // D↔E is mutual → first size-2 group gets D,E? Scan order for mutual: A not mutual, B not, C not, D↔E.
        // But seed uses PopFirst of mutual pairs found greedily - A has no mutual, B no, C no, D↔E.
        // First group size 2: mutual D,E. Wait - FindMutualPairs scans A first - A wishes B, B doesn't wish A. No.
        // Actually first pair found: D↔E. Groups: [D,E], then remaining A,B,C,F.
        // Second group: no mutuals left among A,B,C (cycle isn't mutual), seed A, BFS→B.
        // Third: C, F.
        Assert.Equal(
            ["100004", "100005"],
            composition.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100001", "100002"],
            composition.Groups[1].Members.Select(student => student.Number));
        Assert.Equal(
            ["100003", "100006"],
            composition.Groups[2].Members.Select(student => student.Number));
    }

    #endregion

    #region BFS fill after seeding

    [Fact]
    public void AfterTriangleSeed_ExpandsBreadthFirstFromEarliestMembers()
    {
        // A→B→C→A; A also wishes D. Size 4 → seed triad then BFS adds D from A.
        Student a = Student.Create("100001", ["100002", "100004"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", ["100001"]);
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        Student g = Student.Create("100007", Array.Empty<string>());
        Student h = Student.Create("100008", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f, g, h };

        var strategy = CreateStrategy(students, [4, 4], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003", "100004"],
            composition.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void InjectedWishPicker_ControlsWhoJoinsAfterTriangleSeed()
    {
        Student a = Student.Create("100001", ["100002", "100004", "100005"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", ["100001"]);
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        Student g = Student.Create("100007", Array.Empty<string>());
        Student h = Student.Create("100008", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f, g, h };

        var pickD = CreateStrategy(
            students,
            [4, 4],
            IdentityShuffle,
            PreferNumberThenFirst("100004"));
        var pickE = CreateStrategy(
            students,
            [4, 4],
            IdentityShuffle,
            PreferNumberThenFirst("100005"));

        GroupComposition withD = pickD.GenerateStream().First();
        GroupComposition withE = pickE.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003", "100004"],
            withD.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100001", "100002", "100003", "100005"],
            withE.Groups[0].Members.Select(student => student.Number));
    }

    #endregion

    #region Helpers

    private static TriadFirstStrategy CreateStrategy(
        IReadOnlyList<Student> students,
        IReadOnlyList<int> groupSizes,
        Func<IReadOnlyList<Student>, IReadOnlyList<Student>>? shuffle = null,
        Func<IReadOnlyList<Student>, Student>? pickWish = null)
    {
        var strategy = new TriadFirstStrategy(
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
