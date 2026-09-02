using Logic.Grouping.Invalidation;
using Logic.Grouping.Invalidation.InvalidationStrategies;
using Logic.Models;

namespace UnitTests.InvalidatorTests;

public class GroupCompositionInvalidatorTests
{
    #region Empty / No Invalidators

    [Fact]
    public void ShouldReject_NoInvalidators_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003", "100005"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var invalidator = new GroupCompositionInvalidator([]);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_NoInvalidators_EmptyComposition_ReturnsFalse()
    {
        var composition = Composition();
        var invalidator = new GroupCompositionInvalidator([]);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    #endregion

    #region Single Invalidator - Boundary / Basic Validity

    [Fact]
    public void ShouldReject_SingleInvalidator_ExactlyAtLimit_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var composition = Composition(Group(studentA, studentB, studentC));
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_OneOverLimit_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003", "100005"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_UnderLimit_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void ShouldReject_SingleInvalidator_ControlledFixture_ScalesWithLimit(int limit, bool expectedReject)
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var invalidator = InvalidatorWithMaxPreviousGroup(limit);

        bool result = invalidator.ShouldReject(composition);

        Assert.Equal(expectedReject, result);
    }

    #endregion

    #region Single Invalidator - Per-Group Scoping

    [Fact]
    public void ShouldReject_SingleInvalidator_FormerGroupmatesSplitAcrossGroups_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003", "100005"]);
        var studentE = StudentWithPreviousGroup("100005", ["100001", "100002", "100003", "100004"]);
        var composition = Composition(
            Group(studentA),
            Group(studentB),
            Group(studentC),
            Group(studentD),
            Group(studentE));
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_AllFormerGroupmatesInSameGroup_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003", "100005"]);
        var studentE = StudentWithPreviousGroup("100005", ["100001", "100002", "100003", "100004"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD, studentE));
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    #endregion

    #region Single Invalidator - Per-Student Independence

    [Fact]
    public void ShouldReject_SingleInvalidator_OnlyViolatingStudentCausesRejection()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003"]);
        var studentE = StudentWithPreviousGroup("100005", ["100006"]);
        var studentF = StudentWithPreviousGroup("100006", ["100005"]);
        var composition = Composition(
            Group(studentA, studentB, studentC, studentD),
            Group(studentE, studentF));
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_AsymmetricPreviousLists_OnlyCountsOwnList()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002"]);
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var invalidator = InvalidatorWithMaxPreviousGroup(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_AsymmetricPreviousLists_NoViolationWhenOnlyReverseListExists()
    {
        var studentA = Student("100001");
        var studentB = StudentWithPreviousGroup("100002", ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = InvalidatorWithMaxPreviousGroup(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    #endregion

    #region Single Invalidator - No Constraint Data

    [Fact]
    public void ShouldReject_SingleInvalidator_NullOrEmptyPreviousGroupMembers_ReturnsFalse()
    {
        var studentA = Student("100001");
        var studentB = Student("100002");
        var studentC = Logic.Models.Student.Create("100003", Array.Empty<string>(), previousGroupMembers: Array.Empty<string>());
        var composition = Composition(Group(studentA, studentB, studentC));
        var invalidator = InvalidatorWithMaxPreviousGroup(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_EmptyComposition_ReturnsFalse()
    {
        var composition = Composition();
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_SingleMemberGroups_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003"]);
        var composition = Composition(Group(studentA), Group(studentB));
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    #endregion

    #region Single Invalidator - Multiple Groups / Multiple Violations

    [Fact]
    public void ShouldReject_SingleInvalidator_ViolationInOneOfSeveralGroups_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003"]);
        var studentE = StudentWithPreviousGroup("100005", ["100006"]);
        var studentF = StudentWithPreviousGroup("100006", ["100005"]);
        var composition = Composition(
            Group(studentA, studentB, studentC, studentD),
            Group(studentE, studentF));
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_ViolationsInMultipleGroups_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002"]);
        var studentD = StudentWithPreviousGroup("100004", ["100005", "100006"]);
        var studentE = StudentWithPreviousGroup("100005", ["100004", "100006"]);
        var studentF = StudentWithPreviousGroup("100006", ["100004", "100005"]);
        var composition = Composition(
            Group(studentA, studentB, studentC),
            Group(studentD, studentE, studentF));
        var invalidator = InvalidatorWithMaxPreviousGroup(1);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    #endregion

    #region Single Invalidator - Limit Edge Cases

    [Fact]
    public void ShouldReject_SingleInvalidator_LimitZero_AnyFormerGroupmateInSameGroup_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = InvalidatorWithMaxPreviousGroup(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_LimitZero_NoFormerGroupmatesInSameGroup_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002"]);
        var studentB = StudentWithPreviousGroup("100003", ["100004"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = InvalidatorWithMaxPreviousGroup(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_LimitEqualsPreviousGroupSize_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003", "100005"]);
        var studentE = StudentWithPreviousGroup("100005", ["100001", "100002", "100003", "100004"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD, studentE));
        var invalidator = InvalidatorWithMaxPreviousGroup(4);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    #endregion

    #region Single Invalidator - Larger classroom scenarios (~30 students)

    [Fact]
    public void ShouldReject_SingleInvalidator_ThirtyStudentsKeptInOriginalGroups_ReturnsTrue()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_ThirtyStudentsKeptInOriginalGroups_ReturnsFalse()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var invalidator = InvalidatorWithMaxPreviousGroup(4);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_ThirtyStudentsOptimallyShuffled_ReturnsTrue()
    {
        GroupComposition composition = CreateThirtyStudentRoundRobinComposition();
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_ThirtyStudentsWellShuffled_ReturnsFalse()
    {
        GroupComposition composition = CreateThirtyStudentWellShuffledComposition();
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_SingleInvalidator_ThirtyStudentsPartialOverlap_ReturnsTrue()
    {
        GroupComposition composition = CreateThirtyStudentPartialOverlapComposition();
        var invalidator = InvalidatorWithMaxPreviousGroup(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    #endregion

    #region Combined Invalidators (OR semantics)

    [Fact]
    public void ShouldReject_AnyInvalidatorRejects_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = new GroupCompositionInvalidator(
        [
            new MaxNumberOfStudentsFromPreviousGroup(2),
            new MaxNumberOfStudentsFromPreviousGroup(0),
        ]);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_AllInvalidatorsPass_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = new GroupCompositionInvalidator(
        [
            new MaxNumberOfStudentsFromPreviousGroup(2),
            new MaxNumberOfStudentsFromPreviousGroup(1),
        ]);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_MultipleRejectingInvalidators_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var invalidator = new GroupCompositionInvalidator(
        [
            new MaxNumberOfStudentsFromPreviousGroup(2),
            new MaxNumberOfStudentsFromPreviousGroup(1),
        ]);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_OrderDoesNotAffectResult()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var strictFirst = new GroupCompositionInvalidator(
        [
            new MaxNumberOfStudentsFromPreviousGroup(2),
            new MaxNumberOfStudentsFromPreviousGroup(4),
        ]);
        var lenientFirst = new GroupCompositionInvalidator(
        [
            new MaxNumberOfStudentsFromPreviousGroup(4),
            new MaxNumberOfStudentsFromPreviousGroup(2),
        ]);

        bool strictFirstResult = strictFirst.ShouldReject(composition);
        bool lenientFirstResult = lenientFirst.ShouldReject(composition);

        Assert.True(strictFirstResult);
        Assert.Equal(strictFirstResult, lenientFirstResult);
    }

    [Fact]
    public void ShouldReject_ShortCircuitsWhenEarlierInvalidatorRejects()
    {
        var composition = Composition();
        var recordingInvalidator = new RecordingInvalidator(shouldReject: false);
        var invalidator = new GroupCompositionInvalidator(
        [
            new FixedResultInvalidator(shouldReject: true),
            recordingInvalidator,
        ]);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
        Assert.Equal(0, recordingInvalidator.CallCount);
    }

    [Fact]
    public void ShouldReject_EvaluatesRemainingInvalidatorsWhenEarlierOnesPass()
    {
        var composition = Composition();
        var recordingInvalidator = new RecordingInvalidator(shouldReject: true);
        var invalidator = new GroupCompositionInvalidator(
        [
            new FixedResultInvalidator(shouldReject: false),
            recordingInvalidator,
        ]);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
        Assert.Equal(1, recordingInvalidator.CallCount);
    }

    [Fact]
    public void ShouldReject_AllInvalidatorsPass_EvaluatesEntireChain()
    {
        var composition = Composition();
        var recordingInvalidator = new RecordingInvalidator(shouldReject: false);
        var invalidator = new GroupCompositionInvalidator(
        [
            new FixedResultInvalidator(shouldReject: false),
            new FixedResultInvalidator(shouldReject: false),
            recordingInvalidator,
        ]);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
        Assert.Equal(1, recordingInvalidator.CallCount);
    }

    [Fact]
    public void ShouldReject_MatchesIndividualInvalidatorOutcomes()
    {
        GroupComposition composition = CreateThirtyStudentPartialOverlapComposition();
        IInvalidator[] strategies =
        [
            new MaxNumberOfStudentsFromPreviousGroup(2),
            new MaxNumberOfStudentsFromPreviousGroup(4),
        ];
        var invalidator = new GroupCompositionInvalidator(strategies.ToList());

        bool result = invalidator.ShouldReject(composition);
        bool expected = strategies.Any(strategy => strategy.ShouldReject(composition));

        Assert.Equal(expected, result);
        Assert.True(result);
    }

    #endregion

    #region Helpers

    private static Student Student(string number, params string[] positiveWishes)
        => Logic.Models.Student.Create(number, positiveWishes);

    private static Student StudentWithPreviousGroup(
        string number,
        IReadOnlyList<string> previousGroupMembers,
        params string[] positiveWishes)
        => Logic.Models.Student.Create(number, positiveWishes, previousGroupMembers: previousGroupMembers);

    private static GroupComposition Composition(params Group[] groups)
        => new(groups, TotalScore: 0);

    private static Group Group(params Student[] members)
        => new(members);

    private static GroupCompositionInvalidator InvalidatorWithMaxPreviousGroup(int limit)
        => new([new MaxNumberOfStudentsFromPreviousGroup(limit)]);

    private sealed class FixedResultInvalidator(bool shouldReject) : IInvalidator
    {
        public bool ShouldReject(GroupComposition groupComposition) => shouldReject;
    }

    private sealed class RecordingInvalidator(bool shouldReject) : IInvalidator
    {
        public int CallCount { get; private set; }

        public bool ShouldReject(GroupComposition groupComposition)
        {
            CallCount++;
            return shouldReject;
        }
    }

    /// <summary>
    /// 30 students in six groups of five, kept in their original prior groups.
    /// Each student has four previous group members from their prior group-of-five.
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

    /// <summary>
    /// Cyclic shuffle: each new group has at most one student from each prior group.
    /// No student shares a new group with any former groupmate.
    /// </summary>
    private static GroupComposition CreateThirtyStudentWellShuffledComposition()
    {
        IReadOnlyList<Student> students = CreateThirtyStudentClassroom();
        var byNumber = students.ToDictionary(student => student.Number);

        return Composition(
            Group(byNumber["100001"], byNumber["100006"], byNumber["100011"], byNumber["100016"], byNumber["100021"]),
            Group(byNumber["100002"], byNumber["100007"], byNumber["100012"], byNumber["100017"], byNumber["100026"]),
            Group(byNumber["100003"], byNumber["100008"], byNumber["100013"], byNumber["100022"], byNumber["100027"]),
            Group(byNumber["100004"], byNumber["100009"], byNumber["100018"], byNumber["100023"], byNumber["100028"]),
            Group(byNumber["100005"], byNumber["100014"], byNumber["100019"], byNumber["100024"], byNumber["100029"]),
            Group(byNumber["100010"], byNumber["100015"], byNumber["100020"], byNumber["100025"], byNumber["100030"]));
    }

    /// <summary>
    /// Naive round-robin for the first five groups; the sixth new group keeps prior group 6 intact.
    /// </summary>
    private static GroupComposition CreateThirtyStudentRoundRobinComposition()
    {
        IReadOnlyList<Student> students = CreateThirtyStudentClassroom();
        var byNumber = students.ToDictionary(student => student.Number);

        return Composition(
            Group(byNumber["100001"], byNumber["100006"], byNumber["100011"], byNumber["100016"], byNumber["100021"]),
            Group(byNumber["100002"], byNumber["100007"], byNumber["100012"], byNumber["100017"], byNumber["100022"]),
            Group(byNumber["100003"], byNumber["100008"], byNumber["100013"], byNumber["100018"], byNumber["100023"]),
            Group(byNumber["100004"], byNumber["100009"], byNumber["100014"], byNumber["100019"], byNumber["100024"]),
            Group(byNumber["100005"], byNumber["100010"], byNumber["100015"], byNumber["100020"], byNumber["100025"]),
            Group(byNumber["100026"], byNumber["100027"], byNumber["100028"], byNumber["100029"], byNumber["100030"]));
    }

    /// <summary>
    /// Mostly round-robin, but one group clusters three former groupmates from prior group 1.
    /// </summary>
    private static GroupComposition CreateThirtyStudentPartialOverlapComposition()
    {
        IReadOnlyList<Student> students = CreateThirtyStudentClassroom();
        var byNumber = students.ToDictionary(student => student.Number);

        return Composition(
            Group(byNumber["100001"], byNumber["100002"], byNumber["100003"], byNumber["100016"], byNumber["100021"]),
            Group(byNumber["100004"], byNumber["100007"], byNumber["100012"], byNumber["100017"], byNumber["100022"]),
            Group(byNumber["100005"], byNumber["100008"], byNumber["100013"], byNumber["100018"], byNumber["100023"]),
            Group(byNumber["100006"], byNumber["100009"], byNumber["100014"], byNumber["100019"], byNumber["100024"]),
            Group(byNumber["100010"], byNumber["100011"], byNumber["100015"], byNumber["100020"], byNumber["100025"]),
            Group(byNumber["100026"], byNumber["100027"], byNumber["100028"], byNumber["100029"], byNumber["100030"]));
    }

    private static List<Student> CreateThirtyStudentClassroom()
    {
        string[][] priorGroups =
        [
            ["100001", "100002", "100003", "100004", "100005"],
            ["100006", "100007", "100008", "100009", "100010"],
            ["100011", "100012", "100013", "100014", "100015"],
            ["100016", "100017", "100018", "100019", "100020"],
            ["100021", "100022", "100023", "100024", "100025"],
            ["100026", "100027", "100028", "100029", "100030"],
        ];

        var previousMembersByNumber = priorGroups
            .SelectMany(group => group.Select(member => (
                Member: member,
                PreviousMembers: group.Where(other => other != member).ToArray())))
            .ToDictionary(entry => entry.Member, entry => entry.PreviousMembers);

        return previousMembersByNumber
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => StudentWithPreviousGroup(entry.Key, entry.Value))
            .ToList();
    }

    #endregion
}
