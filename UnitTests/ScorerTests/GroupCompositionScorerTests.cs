using Logic.Grouping.Scoring;
using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace UnitTests.ScorerTests;

public class GroupCompositionScorerTests
{
    #region Empty / No Scorers

    [Fact]
    public void Score_NoScorers_ReturnsZeroTotalScore()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        GroupComposition composition = Composition(Group(studentA, studentB));
        var scorer = new GroupCompositionScorer([]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void Score_NoScorers_PreservesGroups()
    {
        var studentA = Student("100001");
        var studentB = Student("100002");
        GroupComposition composition = Composition(Group(studentA), Group(studentB));
        var scorer = new GroupCompositionScorer([]);

        GroupComposition result = scorer.Score(composition);

        Assert.Same(composition.Groups, result.Groups);
        Assert.Equal(2, result.Groups.Count);
    }

    #endregion

    #region Single Scorer

    [Fact]
    public void Score_SingleMutualMatchScorer_ReturnsMutualMatchScore()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        GroupComposition composition = Composition(Group(studentA, studentB));
        var scorer = new GroupCompositionScorer([new MutualMatch(10.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(10.0, result.TotalScore);
    }

    [Fact]
    public void Score_SinglePartialMatchScorer_ReturnsPartialMatchScore()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        GroupComposition composition = Composition(Group(studentA, studentB));
        var scorer = new GroupCompositionScorer([new PartialMatch(10.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(10.0, result.TotalScore);
    }

    [Fact]
    public void Score_SingleScorerWithNoMatchingWishes_ReturnsZero()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        GroupComposition composition = Composition(
            Group(studentA),
            Group(studentB));
        var scorer = new GroupCompositionScorer([new MutualMatch(10.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(0, result.TotalScore);
    }

    #endregion

    #region Combined Scorers (Mix and Match)

    [Fact]
    public void Score_MutualAndPartialScorers_SumsBothContributions()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        GroupComposition composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new GroupCompositionScorer(
        [
            new MutualMatch(10.0),
            new PartialMatch(10.0),
        ]);

        GroupComposition result = scorer.Score(composition);

        // One mutual pair (10) + one one-way wish (10)
        Assert.Equal(20.0, result.TotalScore);
    }

    [Fact]
    public void Score_MutualAndPartialScorers_CountOnlyTheirRespectiveMatchTypes()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        GroupComposition composition = Composition(Group(studentA, studentB));
        var scorer = new GroupCompositionScorer(
        [
            new MutualMatch(10.0),
            new PartialMatch(10.0),
        ]);

        GroupComposition result = scorer.Score(composition);

        // Mutual pair contributes to MutualMatch only; PartialMatch excludes mutual pairs
        Assert.Equal(10.0, result.TotalScore);
    }

    [Theory]
    [InlineData(10.0, 10.0, 20.0)]
    [InlineData(10.0, 5.0, 15.0)]
    [InlineData(5.5, 3.0, 8.5)]
    public void Score_MutualAndPartialScorers_ScalesEachScorerIndependently(
        double mutualPoints,
        double partialPoints,
        double expectedTotal)
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        GroupComposition composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new GroupCompositionScorer(
        [
            new MutualMatch(mutualPoints),
            new PartialMatch(partialPoints),
        ]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(expectedTotal, result.TotalScore);
    }

    [Fact]
    public void Score_MutualAndPartialScorers_OrderDoesNotAffectTotal()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var mutualFirst = new GroupCompositionScorer([new MutualMatch(10.0), new PartialMatch(10.0)]);
        var partialFirst = new GroupCompositionScorer([new PartialMatch(10.0), new MutualMatch(10.0)]);

        GroupComposition resultMutualFirst = mutualFirst.Score(composition);
        GroupComposition resultPartialFirst = partialFirst.Score(composition);

        Assert.Equal(resultMutualFirst.TotalScore, resultPartialFirst.TotalScore);
    }

    [Fact]
    public void Score_OnlyMutualMatchScorer_IgnoresPartialMatchesInComposition()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002");
        GroupComposition composition = Composition(Group(studentA, studentB));
        var scorer = new GroupCompositionScorer([new MutualMatch(10.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void Score_OnlyPartialMatchScorer_IgnoresMutualMatchesInComposition()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        GroupComposition composition = Composition(Group(studentA, studentB));
        var scorer = new GroupCompositionScorer([new PartialMatch(10.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void Score_MultipleGroupsWithMixedWishes_AggregatesAcrossAllGroups()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        var studentC = Student("100003", "100004");
        var studentD = Student("100004");
        var studentE = Student("100005", "100006");
        var studentF = Student("100006", "100005");
        GroupComposition composition = Composition(
            Group(studentA, studentB),
            Group(studentC, studentD),
            Group(studentE, studentF));
        var scorer = new GroupCompositionScorer(
        [
            new MutualMatch(10.0),
            new PartialMatch(10.0),
        ]);

        GroupComposition result = scorer.Score(composition);

        // Two mutual pairs (20) + one one-way wish (10)
        Assert.Equal(30.0, result.TotalScore);
    }

    #endregion

    #region Immutability / Result Shape

    [Fact]
    public void Score_DoesNotMutateInputComposition()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        GroupComposition composition = Composition(Group(studentA, studentB));
        var scorer = new GroupCompositionScorer([new MutualMatch(10.0), new PartialMatch(5.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(0, composition.TotalScore);
        Assert.Equal(10.0, result.TotalScore);
        Assert.NotSame(composition, result);
    }

    [Fact]
    public void Score_PreservesGroupsReferenceFromInput()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        GroupComposition composition = Composition(Group(studentA, studentB));
        var scorer = new GroupCompositionScorer([new MutualMatch(10.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Same(composition.Groups, result.Groups);
    }

    [Fact]
    public void Score_InputWithZeroInitialScore_UsesOnlyScorerContributions()
    {
        var studentA = Student("100001", "100002");
        var studentB = Student("100002", "100001");
        GroupComposition composition = new([Group(studentA, studentB)], TotalScore: 0);
        var scorer = new GroupCompositionScorer([new MutualMatch(7.5)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(7.5, result.TotalScore);
    }

    #endregion

    #region Larger classroom scenarios (~30 students)

    [Fact]
    public void Score_ThirtyStudentsWithBothScorers_SumsMutualAndPartialContributions()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new GroupCompositionScorer(
        [
            new MutualMatch(10.0),
            new PartialMatch(10.0),
        ]);

        GroupComposition result = scorer.Score(composition);

        // 9 in-group mutual pairs (90) + 35 in-group one-way wishes (350)
        Assert.Equal(440.0, result.TotalScore);
    }

    [Fact]
    public void Score_ThirtyStudentsWithBothScorers_MatchesIndividualScorerTotals()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var compositionScorer = new GroupCompositionScorer(
        [
            new MutualMatch(10.0),
            new PartialMatch(10.0),
        ]);
        var mutualScorer = new MutualMatch(10.0);
        var partialScorer = new PartialMatch(10.0);

        GroupComposition result = compositionScorer.Score(composition);
        double expectedTotal = mutualScorer.Evaluate(composition) + partialScorer.Evaluate(composition);

        Assert.Equal(expectedTotal, result.TotalScore);
        Assert.Equal(440.0, result.TotalScore);
    }

    [Theory]
    [InlineData(10.0, 10.0, 440.0)]
    [InlineData(5.5, 5.5, 242.0)]
    [InlineData(10.0, 5.0, 265.0)]
    public void Score_ThirtyStudentsWithBothScorers_ScalesEachScorerIndependently(
        double mutualPoints,
        double partialPoints,
        double expectedTotal)
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new GroupCompositionScorer(
        [
            new MutualMatch(mutualPoints),
            new PartialMatch(partialPoints),
        ]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(expectedTotal, result.TotalScore);
    }

    [Fact]
    public void Score_ThirtyStudentsWithOnlyMutualMatch_ReturnsMutualScoreOnly()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new GroupCompositionScorer([new MutualMatch(10.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(90.0, result.TotalScore);
    }

    [Fact]
    public void Score_ThirtyStudentsWithOnlyPartialMatch_ReturnsPartialScoreOnly()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new GroupCompositionScorer([new PartialMatch(10.0)]);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(350.0, result.TotalScore);
    }

    [Fact]
    public void Score_ThirtyStudentsWithBothScorers_DoesNotDoubleCountWishes()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new GroupCompositionScorer(
        [
            new MutualMatch(10.0),
            new PartialMatch(10.0),
        ]);

        int mutualPairsInSameGroup = CountMutualPairsInSameGroup(composition);
        int directedPartialMatchesInSameGroup = CountDirectedPartialMatchesInSameGroup(composition);

        GroupComposition result = scorer.Score(composition);

        Assert.Equal(9, mutualPairsInSameGroup);
        Assert.Equal(35, directedPartialMatchesInSameGroup);
        Assert.Equal(
            mutualPairsInSameGroup * 10.0 + directedPartialMatchesInSameGroup * 10.0,
            result.TotalScore);
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

    private static int CountMutualPairsInSameGroup(GroupComposition composition)
        => composition.Groups.Sum(group => CountMutualPairsAmong(group.Members));

    private static int CountDirectedPartialMatchesInSameGroup(GroupComposition composition)
        => composition.Groups.Sum(group => CountDirectedPartialMatchesAmong(group.Members));

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
