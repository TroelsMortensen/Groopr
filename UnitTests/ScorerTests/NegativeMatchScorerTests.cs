using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace UnitTests.ScorerTests;

public class NegativeMatchScorerTests
{
    #region Name / Metadata

    [Fact]
    public void Name_ReturnsNegativeMatch()
    {
        var scorer = new NegativeMatch(1.0);

        Assert.Equal("NegativeMatch", scorer.Name);
    }

    #endregion

    #region Basic Negative Match Scoring

    [Fact]
    public void Evaluate_SingleOneWayNegativeWishInSameGroup_ReturnsConfiguredPenalty()
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-10.0, score);
    }

    [Fact]
    public void Evaluate_OneWayNegativeWishFromSecondStudent_ReturnsConfiguredPenalty()
    {
        var studentA = Student("100001");
        var studentB = Student("100002", negativeWishes: ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-10.0, score);
    }

    [Theory]
    [InlineData(10.0, -10.0)]
    [InlineData(5.0, -5.0)]
    [InlineData(5.5, -5.5)]
    public void Evaluate_ConfiguredPoints_ScalesWithConstructor(double points, double expectedScore)
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new NegativeMatch(points);

        double score = scorer.Evaluate(composition);

        Assert.Equal(expectedScore, score);
    }

    #endregion

    #region Directional Independence (Including Mutual Negatives)

    [Fact]
    public void Evaluate_MutualNegativeWishInSameGroup_CountsBothDirections()
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var studentB = Student("100002", negativeWishes: ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-20.0, score);
    }

    [Fact]
    public void Evaluate_ChainOfOneWayNegativeWishes_EachDirectionCountedIndependently()
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var studentB = Student("100002", negativeWishes: ["100003"]);
        var studentC = Student("100003", negativeWishes: ["100004"]);
        var studentD = Student("100004");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-30.0, score);
    }

    [Fact]
    public void Evaluate_TwoIndependentOneWayNegativeWishesInSameGroup_AccumulatesBoth()
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var studentB = Student("100002");
        var studentC = Student("100003", negativeWishes: ["100004"]);
        var studentD = Student("100004");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-20.0, score);
    }

    [Fact]
    public void Evaluate_StudentWithMultipleNegativeWishesInSameGroup_CountsEach()
    {
        var studentA = Student("100001", negativeWishes: ["100002", "100003"]);
        var studentB = Student("100002");
        var studentC = Student("100003");
        var composition = Composition(Group(studentA, studentB, studentC));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-20.0, score);
    }

    #endregion

    #region Cross-Group Exclusion

    [Fact]
    public void Evaluate_OneWayNegativeWishInDifferentGroups_ReturnsZero()
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var studentB = Student("100002");
        var composition = Composition(
            Group(studentA),
            Group(studentB));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_InGroupAndCrossGroupNegativeWishes_CountsSameGroupOnly()
    {
        var studentA = Student("100001", negativeWishes: ["100002", "100004"]);
        var studentB = Student("100002");
        var studentC = Student("100003", negativeWishes: ["100004"]);
        var studentD = Student("100004");
        var composition = Composition(
            Group(studentA, studentB, studentC),
            Group(studentD));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        // Only A→B is in the same group; A→D and C→D are cross-group
        Assert.Equal(-10.0, score);
    }

    [Fact]
    public void Evaluate_NegativeWishForStudentNotInComposition_ReturnsZero()
    {
        var studentA = Student("100001", negativeWishes: ["100099"]);
        var composition = Composition(Group(studentA));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    #endregion

    #region Positive Wishes Are Ignored

    [Fact]
    public void Evaluate_PositiveWishesInSameGroup_DoNotAffectScore()
    {
        var studentA = Student("100001", positiveWishes: ["100002"]);
        var studentB = Student("100002", positiveWishes: ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_MixedPositiveAndNegativeWishes_CountsOnlyNegativeMatches()
    {
        var studentA = Student("100001", positiveWishes: ["100002"], negativeWishes: ["100003"]);
        var studentB = Student("100002", positiveWishes: ["100001"]);
        var studentC = Student("100003");
        var composition = Composition(Group(studentA, studentB, studentC));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-10.0, score);
    }

    #endregion

    #region Multiple Groups and Scaling

    [Fact]
    public void Evaluate_TwoOneWayNegativeWishesAcrossTwoGroups_AccumulatesPenalties()
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var studentB = Student("100002");
        var studentC = Student("100003", negativeWishes: ["100004"]);
        var studentD = Student("100004");
        var composition = Composition(
            Group(studentA, studentB),
            Group(studentC, studentD));
        var scorer = new NegativeMatch(5.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-10.0, score);
    }

    [Theory]
    [InlineData(10.0, -20.0)]
    [InlineData(5.0, -10.0)]
    [InlineData(5.5, -11.0)]
    public void Evaluate_MultipleNegativeMatches_ScalesWithConfiguredPoints(double points, double expectedScore)
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var studentB = Student("100002");
        var studentC = Student("100003", negativeWishes: ["100004"]);
        var studentD = Student("100004");
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var scorer = new NegativeMatch(points);

        double score = scorer.Evaluate(composition);

        Assert.Equal(expectedScore, score);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Evaluate_NoNegativeWishes_ReturnsZero()
    {
        var studentA = Student("100001");
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_EmptyComposition_ReturnsZero()
    {
        var composition = Composition();
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    [Fact]
    public void Evaluate_GroupWithSingleMember_ReturnsZero()
    {
        var studentA = Student("100001", negativeWishes: ["100002"]);
        var composition = Composition(Group(studentA));
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        Assert.Equal(0, score);
    }

    #endregion

    #region Larger classroom scenarios (~30 students)

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedNegativeWishes_CountsOnlyInGroupDirectedWishes()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new NegativeMatch(10.0);

        double score = scorer.Evaluate(composition);

        // 28 directed negative wishes within groups (cross-group wishes excluded)
        Assert.Equal(-280.0, score);
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedNegativeWishes_ScalesWithConfiguredPoints()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new NegativeMatch(5.5);

        double score = scorer.Evaluate(composition);

        Assert.Equal(-154.0, score);
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedNegativeWishes_CountsMutualNegativesAsTwo()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new NegativeMatch(10.0);

        int mutualNegativePairsInSameGroup = CountMutualNegativePairsInSameGroup(composition);
        int directedNegativeMatchesInSameGroup = CountDirectedNegativeMatchesInSameGroup(composition);

        Assert.True(mutualNegativePairsInSameGroup >= 3,
            "Fixture must include several mutual negative pairs between students in the same group.");
        Assert.Equal(28, directedNegativeMatchesInSameGroup);
        Assert.Equal(directedNegativeMatchesInSameGroup * -10.0, scorer.Evaluate(composition));
    }

    [Fact]
    public void Evaluate_ThirtyStudentsWithMixedNegativeWishes_IgnoresCrossGroupNegativeWishes()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var scorer = new NegativeMatch(10.0);

        int crossGroupDirectedNegativeMatches = CountCrossGroupDirectedNegativeMatches(composition);
        int inGroupDirectedNegativeMatches = CountDirectedNegativeMatchesInSameGroup(composition);

        Assert.True(crossGroupDirectedNegativeMatches >= 3,
            "Fixture must include several negative wishes split across groups.");
        Assert.Equal(inGroupDirectedNegativeMatches * -10.0, scorer.Evaluate(composition));
        Assert.NotEqual(
            (inGroupDirectedNegativeMatches + crossGroupDirectedNegativeMatches) * -10.0,
            scorer.Evaluate(composition));
    }

    [Fact]
    public void CreateThirtyStudentClassroom_EachStudentHasOneToThreeNegativeWishes()
    {
        IReadOnlyList<Student> students = CreateThirtyStudentClassroom();

        Assert.Equal(30, students.Count);
        Assert.All(students, student =>
            Assert.InRange(student.NegativeWishes.Count, 1, 3));
    }

    #endregion

    #region Helpers

    private static Student Student(
        string number,
        IEnumerable<string>? positiveWishes = null,
        IEnumerable<string>? negativeWishes = null)
        => Logic.Models.Student.Create(
            number,
            (positiveWishes ?? []).Select(StudentNumber.Create).ToArray(),
            negativeWishes: (negativeWishes ?? []).Select(StudentNumber.Create).ToArray());

    private static GroupComposition Composition(params Group[] groups)
        => new(groups, TotalScore: 0);

    private static Group Group(params Student[] members)
        => new(members);

    /// <summary>
    /// 30 students in six groups of five. Each student has 1–3 negative wishes.
    /// Includes in-group one-way negatives, same-group mutual negatives, and cross-group negatives.
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
        // Groups: 1-5 | 6-10 | 11-15 | 16-20 | 21-25 | 26-30
        // In-group directed negatives (28 total):
        //   G1: 1→2, 2→1, 1→3, 2→4, 3→4, 4→3, 5→3          = 7
        //   G2: 6→7, 7→8, 8→7, 9→10, 10→9                    = 5
        //   G3: 11→12, 12→11, 13→14, 14→13, 15→13            = 5
        //   G4: 16→17, 17→18, 18→17, 19→20                   = 4
        //   G5: 21→22, 22→21, 23→24, 25→23                   = 4
        //   G6: 26→27, 27→28, 28→27                          = 3
        // Cross-group (ignored): 1→6, 5→10, 8→11, 12→16, 15→20, 19→25, 20→26, 25→30, 29→1, 30→5
        var negativeWishesByNumber = new Dictionary<string, string[]>
        {
            ["100001"] = ["100002", "100003", "100006"],       // mutual 1↔2; one-way →3; cross →6
            ["100002"] = ["100001", "100004"],                 // mutual 1↔2; one-way →4
            ["100003"] = ["100004"],                           // mutual 3↔4
            ["100004"] = ["100003"],                           // mutual 3↔4
            ["100005"] = ["100003", "100010"],                 // one-way →3; cross →10
            ["100006"] = ["100007"],                           // one-way →7
            ["100007"] = ["100008"],                           // mutual 7↔8
            ["100008"] = ["100007", "100011"],                 // mutual 7↔8; cross →11
            ["100009"] = ["100010"],                           // mutual 9↔10
            ["100010"] = ["100009"],                           // mutual 9↔10
            ["100011"] = ["100012"],                           // mutual 11↔12
            ["100012"] = ["100011", "100016"],                 // mutual 11↔12; cross →16
            ["100013"] = ["100014"],                           // mutual 13↔14
            ["100014"] = ["100013"],                           // mutual 13↔14
            ["100015"] = ["100013", "100020"],                 // one-way →13; cross →20
            ["100016"] = ["100017"],                           // one-way →17
            ["100017"] = ["100018"],                           // mutual 17↔18
            ["100018"] = ["100017"],                           // mutual 17↔18
            ["100019"] = ["100020", "100025"],                 // one-way →20; cross →25
            ["100020"] = ["100026"],                           // cross →26
            ["100021"] = ["100022"],                           // mutual 21↔22
            ["100022"] = ["100021"],                           // mutual 21↔22
            ["100023"] = ["100024"],                           // one-way →24
            ["100024"] = ["100030"],                           // cross wish kept for variety; not in G5
            ["100025"] = ["100023", "100030"],                 // one-way →23; cross →30
            ["100026"] = ["100027"],                           // one-way →27
            ["100027"] = ["100028"],                           // mutual 27↔28
            ["100028"] = ["100027"],                           // mutual 27↔28
            ["100029"] = ["100001"],                           // cross →1
            ["100030"] = ["100005"],                           // cross →5
        };

        return negativeWishesByNumber
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => Student(entry.Key, negativeWishes: entry.Value))
            .ToList();
    }

    private static int CountDirectedNegativeMatchesInSameGroup(GroupComposition composition)
        => composition.Groups.Sum(group => CountDirectedNegativeMatchesAmong(group.Members));

    private static int CountCrossGroupDirectedNegativeMatches(GroupComposition composition)
    {
        var memberEntries = composition.Groups
            .SelectMany(
                (group, groupIndex) => group.Members.Select(member => (member, groupIndex)))
            .ToList();
        var byNumber = memberEntries.ToDictionary(entry => entry.member.Number);

        return memberEntries.Sum(entryA =>
            entryA.member.NegativeWishes.Count(wish =>
                byNumber.TryGetValue(wish.Value, out var entryB)
                && entryB.groupIndex != entryA.groupIndex));
    }

    private static int CountMutualNegativePairsInSameGroup(GroupComposition composition)
        => composition.Groups.Sum(group => CountMutualNegativePairsAmong(group.Members));

    private static int CountDirectedNegativeMatchesAmong(IReadOnlyList<Student> members)
    {
        var membersByNumber = members.ToDictionary(member => member.Number);

        return members.Sum(student =>
            student.NegativeWishes.Count(wish => membersByNumber.ContainsKey(wish.Value)));
    }

    private static int CountMutualNegativePairsAmong(IReadOnlyList<Student> members)
    {
        var list = members.ToList();

        return list.SelectMany(
                (a, i) => list.Skip(i + 1),
                (a, b) => (a, b))
            .Count(pair =>
                pair.a.NegativeWishes.Any(wish => wish.Value == pair.b.Number)
                && pair.b.NegativeWishes.Any(wish => wish.Value == pair.a.Number));
    }

    #endregion
}
