using Logic.Grouping.Generation;
using Logic.Models;

namespace UnitTests.GenerationTests;

/// <summary>
/// Behavior that is specific to <see cref="MutualPairFirstStrategy"/>
/// (mutual-pair seeding, then BFS wish expansion), not the shared producer contract.
/// </summary>
public class MutualPairFirstStrategyTests
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

    #region Mutual pair seeding

    [Fact]
    public void SeedsGroupWithMutualPair_WhenAvailable()
    {
        // A↔B mutual; C has no mutuals. Group size 3 → seed A,B then fill with C via fallback.
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
        Assert.Equal(
            ["100004", "100005", "100006"],
            composition.Groups[1].Members.Select(student => student.Number));
    }

    [Fact]
    public void UsesSeparateMutualPairs_ForSuccessiveGroups()
    {
        // A↔B and C↔D; sizes [2, 2, 2]
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", ["100004"]);
        Student d = Student.Create("100004", ["100003"]);
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        var strategy = CreateStrategy(students, [2, 2, 2], IdentityShuffle, PickFirst);

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
    public void OneWayWish_IsNotTreatedAsMutualPair()
    {
        // A→B only (not mutual); identity shuffle seeds with A then BFS to B.
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
    public void NoMutualPairs_FallsBackToSingleSeedThenBfs()
    {
        // Same outcome as BFS wish-chain when nobody is mutual.
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
    public void TargetSizeOne_DoesNotSeedWithMutualPair()
    {
        Student a = Student.Create("100001", ["100002"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        var students = new List<Student> { a, b, c };

        var strategy = CreateStrategy(students, [1, 1, 1], IdentityShuffle, PickFirst);

        GroupComposition composition = strategy.GenerateStream().First();

        Assert.All(composition.Groups, group => Assert.Single(group.Members));
        Assert.Equal(
            ["100001", "100002", "100003"],
            composition.Groups.SelectMany(group => group.Members).Select(student => student.Number));
    }

    #endregion

    #region BFS fill after seeding

    [Fact]
    public void AfterMutualSeed_ExpandsBreadthFirstFromEarliestMembers()
    {
        // A↔B mutual. A also wishes C; B wishes D. Size 3 → seed A,B then BFS prefers A's remaining C over B's D.
        Student a = Student.Create("100001", ["100002", "100003"]);
        Student b = Student.Create("100002", ["100001", "100004"]);
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
    public void InjectedWishPicker_ControlsWhoJoinsAfterSeed()
    {
        Student a = Student.Create("100001", ["100002", "100003", "100004"]);
        Student b = Student.Create("100002", ["100001"]);
        Student c = Student.Create("100003", Array.Empty<string>());
        Student d = Student.Create("100004", Array.Empty<string>());
        Student e = Student.Create("100005", Array.Empty<string>());
        Student f = Student.Create("100006", Array.Empty<string>());
        var students = new List<Student> { a, b, c, d, e, f };

        // Mutual A↔B seeds first two; then PickWish among A's remaining wishes.
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

        Assert.Equal(
            ["100001", "100002", "100003"],
            withC.Groups[0].Members.Select(student => student.Number));
        Assert.Equal(
            ["100001", "100002", "100004"],
            withD.Groups[0].Members.Select(student => student.Number));
    }

    [Fact]
    public void ExhaustedWishes_FallsBackToNextUnassigned()
    {
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

    #endregion

    #region Helpers

    private static MutualPairFirstStrategy CreateStrategy(
        IReadOnlyList<Student> students,
        IReadOnlyList<int> groupSizes,
        Func<IReadOnlyList<Student>, IReadOnlyList<Student>>? shuffle = null,
        Func<IReadOnlyList<Student>, Student>? pickWish = null)
    {
        var strategy = new MutualPairFirstStrategy(
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
