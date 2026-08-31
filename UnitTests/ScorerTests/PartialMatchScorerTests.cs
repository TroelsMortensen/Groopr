using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace UnitTests.ScorerTests;

public class PartialMatchScorerTests
{
    #region Name / Metadata

    [Fact]
    public void Name_ReturnsPartialMatch()
    {
        var scorer = new PartialMatch(1.0);

        Assert.Equal("PartialMatch", scorer.Name);
    }

    #endregion

    #region One-Way (Partial) Match Scoring

    [Fact]
    public void Evaluate_SingleOneWayWishInSameGroup_ReturnsConfiguredPoints()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(10.0, score);
    }

    [Fact]
    public void Evaluate_OneWayWishFromSecondStudent_ReturnsConfiguredPoints()
    {
        var studentA = Student("100001");
        var studentB = Student("100002", "100001");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(10.0, score);
    }

    [Theory]
    [InlineData(10.0, 10.0)]
    [InlineData(5.0, 5.0)]
    [InlineData(5.5, 5.5)]
    public void Evaluate_ConfiguredPoints_ScalesWithConstructor(double points, double expectedScore)
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new PartialMatch(points);

        double score = scorer.Evaluate(composition);

        Assert.Equal(expectedScore, score);
    }

    #endregion

    #region Exclusion of Mutual (Full) Matches

    [Fact]
    public void Evaluate_MutualPairInSameGroup_ReturnsZero()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_MixedMutualAndOneWayInSameGroup_CountsOnlyOneWayWishes()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(10.0, score);
    }

    [Fact]
    public void Evaluate_OneWayWishBetweenPairWithOtherMutualPartner_ReturnsPartialOnly()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100003");
        var studentC = Student("100003", "100002");
        var composition = Composition(Group(studentA, studentB, studentC));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(10.0, score);
    }

    #endregion

    #region Cross-Group Exclusion

    [Fact]
    public void Evaluate_OneWayWishInDifferentGroups_ReturnsZero()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        var composition = Composition(
            Group(studentA),
            Group(studentB));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_InGroupAndCrossGroupOneWayWishes_CountsSameGroupOnly()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        var composition = Composition(
            Group(studentA, studentB, studentC),
            Group(studentD));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(10.0, score);
    }

    [Fact]
    public void Evaluate_WishForStudentNotInGroup_ReturnsZero()
    {
        var studentA = Student("100001", "100099");
        var composition = Composition(Group(studentA));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    #endregion

    #region Directional Independence

    [Fact]
    public void Evaluate_ChainOfOneWayWishes_EachDirectionCountedIndependently()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100003");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(30.0, score);
    }

    [Fact]
    public void Evaluate_TwoIndependentOneWayWishesInSameGroup_AccumulatesBoth()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(20.0, score);
    }

    #endregion

    #region Multiple Partial Matches and Scaling

    [Fact]
    public void Evaluate_TwoOneWayWishesAcrossTwoGroups_AccumulatesPoints()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        var composition = Composition(
            Group(studentA, studentB),
            Group(studentC, studentD));
        var scorer = new PartialMatch(5.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(10.0, score);
    }

    [Theory]
    [InlineData(10.0, 20.0)]
    [InlineData(5.0, 10.0)]
    [InlineData(5.5, 11.0)]
    public void Evaluate_MultiplePartialMatches_ScalesWithConfiguredPoints(double points, double expectedScore)
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new PartialMatch(points);

        double score = scorer.Evaluate(composition);

        Assert.Equal(expectedScore, score);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Evaluate_NoPositiveWishes_ReturnsZero()
    {
        var studentA = Student("100001");
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_EmptyComposition_ReturnsZero()
    {
        var composition = Composition();
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_GroupWithSingleMember_ReturnsZero()
    {
        var studentA = Student("100001", "100002");
        var composition = Composition(Group(studentA));
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    #endregion

    #region Larger classroom scenarios (~30 students)

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedWishes_CountsOnlyInGroupOneWayWishes()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new PartialMatch(10.0);

        double score = scorer.Evaluate(composition);

        // 35 directed one-way wishes within groups (mutual pairs and cross-group wishes excluded)
        Assert.Equal(350.0, score);
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedWishes_ScalesWithConfiguredPoints()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new PartialMatch(5.5);

        double score = scorer.Evaluate(composition);

        Assert.Equal(192.5, score);
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedWishes_IgnoresMutualMatchesInSameGroup()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new PartialMatch(10.0);

        int mutualPairsInSameGroup = CountMutualPairsInSameGroup(composition);
        int directedPartialMatchesInSameGroup = CountDirectedPartialMatchesInSameGroup(composition);

        Assert.True(mutualPairsInSameGroup >= 3,
            "Fixture must include several mutual pairs between students in the same group.");
        Assert.Equal(35, directedPartialMatchesInSameGroup);
        Assert.Equal(directedPartialMatchesInSameGroup * 10.0, scorer.Evaluate(composition));
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedWishes_IgnoresCrossGroupOneWayWishes()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new PartialMatch(10.0);

        int crossGroupDirectedPartialMatches = CountCrossGroupDirectedPartialMatches(composition);
        int inGroupDirectedPartialMatches = CountDirectedPartialMatchesInSameGroup(composition);

        Assert.True(crossGroupDirectedPartialMatches >= 3,
            "Fixture must include several one-way wishes split across groups.");
        Assert.Equal(inGroupDirectedPartialMatches * 10.0, scorer.Evaluate(composition));
        Assert.NotEqual((inGroupDirectedPartialMatches + crossGroupDirectedPartialMatches) * 10.0,
            scorer.Evaluate(composition));
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
    /// Includes in-group mutual pairs, same-group one-way wishes, and cross-group wishes.
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

    private static int CountDirectedPartialMatchesInSameGroup(GroupComposition composition)
        => composition.Groups.Sum(group => CountDirectedPartialMatchesAmong(group.Members));

    private static int CountCrossGroupDirectedPartialMatches(GroupComposition composition)
    {
        var memberEntries = composition.Groups
            .SelectMany(
                (group, groupIndex) => group.Members.Select(member => (member, groupIndex)))
            .ToList();
        var byNumber = memberEntries.ToDictionary(entry => entry.member.Number);

        return memberEntries.Sum(entryA =>
            entryA.member.PositiveWishes.Count(wish =>
                byNumber.TryGetValue(wish.Value, out var entryB)
                && entryB.groupIndex != entryA.groupIndex
                && !entryB.member.PositiveWishes.Any(other => other.Value == entryA.member.Number)));
    }

    private static int CountMutualPairsInSameGroup(GroupComposition composition)
        => composition.Groups.Sum(group => CountMutualPairsAmong(group.Members));

    private static int CountDirectedPartialMatchesAmong(IReadOnlyList<Student> members)
    {
        var membersByNumber = members.ToDictionary(member => member.Number);

        return members.Sum(student =>
            student.PositiveWishes.Count(wish =>
                membersByNumber.TryGetValue(wish.Value, out Student? wishedStudent)
                && !wishedStudent.PositiveWishes.Any(other => other.Value == student.Number)));
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

    #endregion
}
