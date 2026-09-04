using Logic.Grouping.Generation;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior that is specific to <see cref="OrphanFirstStrategy"/>
/// (seed least-wished students first, then BFS expansion), not the shared producer contract.
/// </summary>
public class OrphanFirstStrategyTests
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

    #region Orphan seeding

    [Fact]
    public void SeedsWithLeastIncomingWishesFirst()
    {
        // Incoming: C=0, A=1 (from C), B=2 (from A and C). Seed first group with C.
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", Array.Empty<string>());
        Student c = Student.Create("100003", ["100001", "100002"]);
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        // Put popular students first so identity-shuffle alone would seed A; orphan-sort should seed C.
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal("100003", composition.Groups[0].Members[0].Number);
    }

    [Fact]
    public void AfterOrphanSeed_ExpandsBreadthFirstAlongWishes()
    {
        // Incoming: C=0, others higher. C wishes A, A wishes B → group [C, A, B].
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", Array.Empty<string>());
        Student c = Student.Create("100003", ["100001"]);
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100003", "100001", "100002"],
            composition.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void EqualIncomingCounts_PreservesShuffleTieOrder()
    {
        // No wishes anywhere → all incoming 0; identity shuffle keeps input order for seeding.
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

    [Fact]
    public void SecondGroup_AlsoSeedsMostIsolatedRemaining()
    {
        // Incoming: A=0, B=1, C=2, D=0, E=0, F=0.
        // Stable orphan order after identity shuffle: A, D, E, F, B, C.
        Student a = Student.Create("100001", ["100002", "100003"]);
        Student b = Student.Create("100002", ["100003"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        // Group 1 seeds A, BFS A→B→C.
        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
        // Remaining D,E,F; seed D.
        Assert.Equal(
            ["100004", "100005", "100006"],
            composition.Groups[1].Members.Select(student => student.Number));
    }

    #endregion

    #region BFS fill / PickWish / fallback

    [Fact]
    public void InjectedWishPicker_ControlsWhoJoinsAfterSeed()
    {
        // All incoming 0 except B,C,D get 1 from A. Seed is... A wishes B,C,D so A has incoming 0,
        // B,C,D have 1, E,F have 0. Stable order among 0: A, E, F.
        Student a = Student.Create("100001", ["100002", "100003", "100004"]);
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

    [Fact]
    public void OrphanWithNoWishes_FillsFromRemainingOrphanOrder()
    {
        // A is orphan (incoming 0) with no outgoing; B wished by nobody with no outgoing...
        // All zero incoming, identity order → [A,B,C] then [D,E,F].
        var students = CreateStudents(6);
        var strategy = CreateStrategy(students, [3, 3], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void FirstExpansion_OffersAllAvailableWishesOfSeed()
    {
        Student a = Student.Create("100001", ["100002", "100003", "100004"]);
        Student b = Student.Create("100002", Array.Empty<string>());
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var recordingPicker = new RecordingWishPicker(candidates => candidates[0]);
        var strategy = CreateStrategy(
            students,
            [3, 3],
            IdentityShuffle,
            recordingPicker.Invoke);

        _ = strategy.GenerateStream().First();

        Assert.NotEmpty(recordingPicker.CandidateSets);
        string[] firstPickCandidates = recordingPicker.CandidateSets[0]
            .Select(student => student.Number)
            .OrderBy(number => number, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["100002", "100003", "100004"], firstPickCandidates);
    }

    #endregion

    #region Helpers

    private static OrphanFirstStrategy CreateStrategy(
        IReadOnlyList<Student> students,
        IReadOnlyList<int> groupSizes,
        Func<IReadOnlyList<Student>, IReadOnlyList<Student>>? shuffle = null,
        Func<IReadOnlyList<Student>, Student>? pickWish = null)
    {
        var strategy = new OrphanFirstStrategy(
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

    private sealed class RecordingWishPicker(Func<IReadOnlyList<Student>, Student> inner)
    {
        public List<IReadOnlyList<Student>> CandidateSets { get; } = [];

        public Student Invoke(IReadOnlyList<Student> candidates)
        {
            CandidateSets.Add(candidates.ToList());
            return inner(candidates);
        }
    }

    #endregion
}
