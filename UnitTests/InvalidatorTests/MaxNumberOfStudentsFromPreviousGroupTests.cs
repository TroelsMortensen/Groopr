using Logic.Grouping.Invalidation.InvalidationStrategies;
using Logic.Models;

namespace UnitTests.InvalidatorTests;

public class MaxNumberOfStudentsFromPreviousGroupTests
{
    #region Boundary / Basic Validity

    [Fact]
    public void ShouldReject_ExactlyAtLimit_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var composition = Composition(Group(studentA, studentB, studentC));
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_OneOverLimit_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003", "100005"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_UnderLimit_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void ShouldReject_ControlledFixture_ScalesWithLimit(int limit, bool expectedReject)
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD));
        var invalidator = Invalidator(limit);

        bool result = invalidator.ShouldReject(composition);

        Assert.Equal(expectedReject, result);
    }

    #endregion

    #region Per-Group Scoping

    [Fact]
    public void ShouldReject_FormerGroupmatesSplitAcrossGroups_ReturnsFalse()
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
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_AllFormerGroupmatesInSameGroup_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003", "100005"]);
        var studentE = StudentWithPreviousGroup("100005", ["100001", "100002", "100003", "100004"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD, studentE));
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    #endregion

    #region Per-Student Independence

    [Fact]
    public void ShouldReject_OnlyViolatingStudentCausesRejection()
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
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_AsymmetricPreviousLists_OnlyCountsOwnList()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002"]);
        var studentB = Student("100002");
        var composition = Composition(Group(studentA, studentB));
        var invalidator = Invalidator(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_AsymmetricPreviousLists_NoViolationWhenOnlyReverseListExists()
    {
        var studentA = Student("100001");
        var studentB = StudentWithPreviousGroup("100002", ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        int allowedPreviousGroupMembers = 0;
        var invalidator = Invalidator(allowedPreviousGroupMembers);

        bool shouldBeTrue = invalidator.ShouldReject(composition);

        Assert.True(shouldBeTrue);
    }

    #endregion

    #region No Constraint Data

    [Fact]
    public void ShouldReject_NullOrEmptyPreviousGroupMembers_ReturnsFalse()
    {
        var studentA = Student("100001");
        var studentB = Student("100002");
        var studentC = Logic.Models.Student.Create("100003", Array.Empty<string>(), previousGroupMembers: Array.Empty<string>());
        var composition = Composition(Group(studentA, studentB, studentC));
        var invalidator = Invalidator(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_EmptyComposition_ReturnsFalse()
    {
        var composition = Composition();
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_SingleMemberGroups_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003"]);
        var composition = Composition(Group(studentA), Group(studentB));
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    #endregion

    #region Multiple Groups / Multiple Violations

    [Fact]
    public void ShouldReject_ViolationInOneOfSeveralGroups_ReturnsTrue()
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
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_ViolationsInMultipleGroups_ReturnsTrue()
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
        var invalidator = Invalidator(1);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    #endregion

    #region Limit Edge Cases

    [Fact]
    public void ShouldReject_LimitZero_AnyFormerGroupmateInSameGroup_ReturnsTrue()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = Invalidator(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_LimitZero_NoFormerGroupmatesInSameGroup_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002"]);
        var studentB = StudentWithPreviousGroup("100003", ["100004"]);
        var composition = Composition(Group(studentA, studentB));
        var invalidator = Invalidator(0);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_LimitEqualsPreviousGroupSize_ReturnsFalse()
    {
        var studentA = StudentWithPreviousGroup("100001", ["100002", "100003", "100004", "100005"]);
        var studentB = StudentWithPreviousGroup("100002", ["100001", "100003", "100004", "100005"]);
        var studentC = StudentWithPreviousGroup("100003", ["100001", "100002", "100004", "100005"]);
        var studentD = StudentWithPreviousGroup("100004", ["100001", "100002", "100003", "100005"]);
        var studentE = StudentWithPreviousGroup("100005", ["100001", "100002", "100003", "100004"]);
        var composition = Composition(Group(studentA, studentB, studentC, studentD, studentE));
        var invalidator = Invalidator(4);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    #endregion

    #region Larger classroom scenarios (~30 students)

    [Fact]
    public void ShouldReject_ThirtyStudentsKeptInOriginalGroups_ReturnsTrue()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReject_ThirtyStudentsKeptInOriginalGroups_ReturnsFalse()
    {
        GroupComposition composition = CreateThirtyStudentClassroomComposition();
        var invalidator = Invalidator(4);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_ThirtyStudentsOptimallyShuffled_ReturnsTrue()
    {
        GroupComposition composition = CreateThirtyStudentRoundRobinComposition();
        var invalidator = Invalidator(2);

        bool shouldBeTrue = invalidator.ShouldReject(composition);

        Assert.True(shouldBeTrue);
    }

    [Fact]
    public void ShouldReject_ThirtyStudentsWellShuffled_ReturnsFalse()
    {
        GroupComposition composition = CreateThirtyStudentWellShuffledComposition();
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.False(result);
    }

    [Fact]
    public void ShouldReject_ThirtyStudentsPartialOverlap_ReturnsTrue()
    {
        GroupComposition composition = CreateThirtyStudentPartialOverlapComposition();
        var invalidator = Invalidator(2);

        bool result = invalidator.ShouldReject(composition);

        Assert.True(result);
    }

    [Fact]
    public void CreateThirtyStudentClassroom_EachStudentHasFourPreviousGroupMembers()
    {
        IReadOnlyList<Student> students = CreateThirtyStudentClassroom();

        Assert.Equal(30, students.Count);
        Assert.All(students, student =>
            Assert.Equal(4, student.PreviousGroupMembers.Count));
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

    private static MaxNumberOfStudentsFromPreviousGroup Invalidator(int limit)
        => new(limit);

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
