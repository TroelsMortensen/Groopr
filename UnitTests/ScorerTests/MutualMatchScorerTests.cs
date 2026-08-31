using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace UnitTests.ScorerTests;

public class MutualMatchScorerTests
{
    #region Name / Metadata

    [Fact]
    public void Name_ReturnsMutualMatch()
    {
        var scorer = new MutualMatch(1.0);

        Assert.Equal("MutualMatch", scorer.Name);
    }

    #endregion

    #region Basic Mutual Match Scoring

    [Fact]
    public void Evaluate_SingleMutualPairInSameGroup_ReturnsConfiguredPoints()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(10.0, score);
    }

    [Theory]
    [InlineData(10.0, 10.0)]
    [InlineData(5.5, 5.5)]
    [InlineData(1.0, 1.0)]
    public void Evaluate_ConfiguredPoints_ScalesWithConstructor(double points, double expectedScore)
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new MutualMatch(points);

        double score = scorer.Evaluate(composition);

        Assert.Equal(expectedScore, score);
    }

    #endregion

    #region No Double Counting (Symmetry Protection)

    [Fact]
    public void Evaluate_MutualPair_IsCountedOnceNotTwice()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new MutualMatch(7.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(7.0, score);
    }

    #endregion

    #region Cross-Group Exclusion

    [Fact]
    public void Evaluate_MutualWishesInDifferentGroups_ReturnsZero()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var composition = Composition(
            Group(studentA),
            Group(studentB));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_OneMutualPairInGroupAndCrossGroupPair_ReturnsPointsForSameGroupOnly()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004", "100003");
        var composition = Composition(
            Group(studentA, studentB, studentC),
            Group(studentD));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(10.0, score);
    }

    #endregion

    #region Unilateral (One-Way) Wishes

    [Fact]
    public void Evaluate_OnlyFirstStudentListsSecond_ReturnsZero()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_OnlySecondStudentListsFirst_ReturnsZero()
    {
        var studentA = Student("100001");
        var studentB = Student("100002", "100001");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_BothListEachOtherButOnlyOneDirectionInSameGroup_ReturnsZero()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100003");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    #endregion

    #region Multiple Pairs and Scaling

    [Fact]
    public void Evaluate_TwoMutualPairsInSameGroup_AccumulatesPoints()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004", "100003");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(20.0, score);
    }

    [Fact]
    public void Evaluate_TwoMutualPairsAcrossTwoGroups_AccumulatesPoints()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004", "100003");
        var composition = Composition(
            Group(studentA, studentB),
            Group(studentC, studentD));
        var scorer = new MutualMatch(5.5);

        double score = scorer.Evaluate(composition);

        Assert.Equal(11.0, score);
    }

    [Theory]
    [InlineData(10.0, 20.0)]
    [InlineData(5.5, 11.0)]
    public void Evaluate_MultiplePairs_ScalesWithConfiguredPoints(double points, double expectedScore)
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004", "100003");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new MutualMatch(points);

        double score = scorer.Evaluate(composition);

        Assert.Equal(expectedScore, score);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Evaluate_NoMutualMatchesInGroup_ReturnsZero()
    {
        var studentA = Student("100001", "100003");
        var studentB = Student("100002", "100004");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_EmptyComposition_ReturnsZero()
    {
        var composition = Composition();
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_GroupWithSingleMember_ReturnsZero()
    {
        var studentA = Student("100001", "100002");
        var composition = Composition(Group(studentA));
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    #endregion

    #region Larger classroom scenarios (~30 students)

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedWishes_CountsOnlyInGroupMutualPairs()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new MutualMatch(10.0);

        double score = scorer.Evaluate(composition);

        // 9 in-group mutual pairs: (1,2), (3,4), (7,8), (9,10), (11,12), (13,14), (17,18), (21,22), (27,28)
        Assert.Equal(90.0, score);
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedWishes_ScalesWithConfiguredPoints()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new MutualMatch(5.5);

        double score = scorer.Evaluate(composition);

        Assert.Equal(49.5, score);
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedWishes_IgnoresPartialMatchesInSameGroup()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new MutualMatch(10.0);

        int partialPairsInSameGroup = CountPartialPairsInSameGroup(composition);
        int mutualPairsInSameGroup = CountMutualPairsInSameGroup(composition);

        Assert.True(partialPairsInSameGroup >= 3,
            "Fixture must include several one-way wishes between students in the same group.");
        Assert.Equal(9, mutualPairsInSameGroup);
        Assert.Equal(mutualPairsInSameGroup * 10.0, scorer.Evaluate(composition));
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedWishes_IgnoresCrossGroupMutualPairs()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new MutualMatch(10.0);

        int crossGroupMutualPairs = CountCrossGroupMutualPairs(composition);
        int inGroupMutualPairs = CountMutualPairsInSameGroup(composition);

        Assert.True(crossGroupMutualPairs >= 1,
            "Fixture must include at least one mutual pair split across groups.");
        Assert.Equal(inGroupMutualPairs * 10.0, scorer.Evaluate(composition));
        Assert.NotEqual((inGroupMutualPairs + crossGroupMutualPairs) * 10.0, scorer.Evaluate(composition));
    }

    [Fact]
    public void CreateThirtyStudentClassroom_EachStudentHasTwoOrThreeWishes()
    {
        IReadOnlyList<Student> students = CreateThirtyStudentClassroom();

        Assert.Equal(30, students.Count);
        Assert.All(students, student =>
            Assert.InRange(student.PositiveWishes.Count, 2, 3));
    }

    #endregion

    #region Helpers

    private static Student Student(string number, params string[] positiveWishes)
        => Logic.Models.Student.Create(number, positiveWishes.Select(StudentNumber.Create).ToArray());

    private static GroupComposition Composition(params Group[] groups)
        => new(groups, TotalScore: 0);

    private static Group Group(params Student[] members)
        => new(members);

    /// <summary>
    /// 30 students in six groups of five. Each student has 2–3 wishes.
    /// Includes in-group mutual pairs, same-group one-way wishes, and cross-group mutual pairs.
    /// </summary>
    private static GroupComposition CreateThirtyStudentClassroomComposition()
    {
        IReadOnlyList<Student> students = CreateThirtyStudentClassroom();
        var byNumber = students.ToDictionary(student => student.Number);

        return Composition(
            Group(byNumber["100001"], byNumber["100002"], byNumber["100003"], byNumber["100004"], byNumber["100005"]),
            Group(byNumber["100006"], byNumber["100007"], byNumber["100008"], byNumber["100009"], byNumber["100010"]),
            Group(byNumber["100011"], byNumber["100012"], byNumber["100013"], byNumber["100014"], byNumber["100015"]),
            Group(byNumber["100016"], byNumber["100017"], byNumber["100018"], byNumber["100019"], byNumber["100020"]),
            Group(byNumber["100021"], byNumber["100022"], byNumber["100023"], byNumber["100024"], byNumber["100025"]),
            Group(byNumber["100026"], byNumber["100027"], byNumber["100028"], byNumber["100029"], byNumber["100030"]));
    }

    private static List<Student> CreateThirtyStudentClassroom()
    {
        var wishesByNumber = new Dictionary<string, string[]>
        {
            ["100001"] = ["100002", "100003", "100020"],       // mutual 1↔2; one-way →3
            ["100002"] = ["100001", "100004", "100021"],       // mutual 1↔2; one-way →4
            ["100003"] = ["100004", "100005", "100006"],       // mutual 3↔4
            ["100004"] = ["100003", "100007", "100008"],
            ["100005"] = ["100006", "100007", "100008"],       // one-way →6 (6 does not wish 5)
            ["100006"] = ["100007", "100008", "100009"],
            ["100007"] = ["100008", "100009", "100010"],       // mutual 7↔8
            ["100008"] = ["100007", "100009", "100011"],
            ["100009"] = ["100010", "100011", "100012"],       // mutual 9↔10
            ["100010"] = ["100009", "100013", "100014"],
            ["100011"] = ["100012", "100013", "100014"],       // mutual 11↔12
            ["100012"] = ["100011", "100015", "100016"],
            ["100013"] = ["100014", "100015", "100016"],       // mutual 13↔14
            ["100014"] = ["100013", "100017", "100018"],
            ["100015"] = ["100016", "100017", "100018"],       // mutual 15↔16 (cross-group)
            ["100016"] = ["100015", "100017", "100019"],
            ["100017"] = ["100018", "100019", "100020"],       // mutual 17↔18
            ["100018"] = ["100017", "100019", "100020"],
            ["100019"] = ["100020", "100021", "100022"],       // one-way →20
            ["100020"] = ["100021", "100022", "100023"],
            ["100021"] = ["100022", "100023", "100024"],       // mutual 21↔22
            ["100022"] = ["100021", "100023", "100024"],
            ["100023"] = ["100024", "100025", "100026"],       // one-way →24
            ["100024"] = ["100025", "100026", "100027"],
            ["100025"] = ["100026", "100027", "100028"],       // one-way →26 (26 does not wish 25)
            ["100026"] = ["100027", "100028", "100029"],
            ["100027"] = ["100028", "100029", "100030"],       // mutual 27↔28
            ["100028"] = ["100027", "100029", "100030"],
            ["100029"] = ["100030", "100001", "100002"],       // one-way →30
            ["100030"] = ["100001", "100002", "100003"],
        };

        return wishesByNumber
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => Student(entry.Key, entry.Value))
            .ToList();
    }

    private static int CountMutualPairsInSameGroup(GroupComposition composition)
        => composition.Groups.Sum(group => CountMutualPairsAmong(group.Members));

    private static int CountPartialPairsInSameGroup(GroupComposition composition)
        => composition.Groups.Sum(group => CountPartialPairsAmong(group.Members));

    private static int CountCrossGroupMutualPairs(GroupComposition composition)
    {
        var memberEntries = composition.Groups
            .SelectMany(
                (group, groupIndex) => group.Members.Select(member => (member, groupIndex)))
            .ToList();

        return memberEntries
            .SelectMany(
                (entryA, index) => memberEntries.Skip(index + 1),
                (entryA, entryB) => (entryA, entryB))
            .Count(pair =>
                pair.entryA.groupIndex != pair.entryB.groupIndex
                && pair.entryA.member.PositiveWishes.Any(wish => wish.Value == pair.entryB.member.Number)
                && pair.entryB.member.PositiveWishes.Any(wish => wish.Value == pair.entryA.member.Number));
    }

    private static int CountMutualPairsAmong(IReadOnlyList<Student> members)
    {
        var list = members.ToList();

        return list.SelectMany(
                (a, i) => list.Skip(i + 1),
                (a, b) => (a, b))
            .Count(pair =>
                pair.a.PositiveWishes.Any(wish => wish.Value == pair.b.Number)
                && pair.b.PositiveWishes.Any(wish => wish.Value == pair.a.Number));
    }

    private static int CountPartialPairsAmong(IReadOnlyList<Student> members)
    {
        var list = members.ToList();

        return list.SelectMany(
                (a, i) => list.Skip(i + 1),
                (a, b) => (a, b))
            .Count(pair =>
            {
                bool aWishesB = pair.a.PositiveWishes.Any(wish => wish.Value == pair.b.Number);
                bool bWishesA = pair.b.PositiveWishes.Any(wish => wish.Value == pair.a.Number);

                return aWishesB ^ bWishesA;
            });
    }

    #endregion
}
