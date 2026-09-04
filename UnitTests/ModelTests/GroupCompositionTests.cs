using Logic.Models;

namespace UnitTests.ModelTests;

public class GroupCompositionTests
{
    private static readonly IReadOnlyList<StudentNumber> EmptyWishes = [];

    #region Equality – order independence

    [Fact]
    public void Equals_SameGroupsSameOrder_ReturnsTrue()
    {
        var left = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003"), Student("100004")));
        var right = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003"), Student("100004")));

        Assert.Equal(left, right);
    }

    [Fact]
    public void Equals_GroupsInDifferentOrder_ReturnsTrue()
    {
        var left = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003"), Student("100004")));
        var right = Composition(
            Group(Student("100003"), Student("100004")),
            Group(Student("100001"), Student("100002")));

        Assert.Equal(left, right);
    }

    [Fact]
    public void Equals_StudentsInDifferentOrderWithinGroups_ReturnsTrue()
    {
        var left = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003"), Student("100004")));
        var right = Composition(
            Group(Student("100002"), Student("100001")),
            Group(Student("100004"), Student("100003")));

        Assert.Equal(left, right);
    }

    [Fact]
    public void Equals_GroupsAndStudentsBothReordered_ReturnsTrue()
    {
        var left = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003"), Student("100004")));
        var right = Composition(
            Group(Student("100004"), Student("100003")),
            Group(Student("100002"), Student("100001")));

        Assert.Equal(left, right);
    }

    #endregion

    #region Equality – irrelevant data ignored

    [Fact]
    public void Equals_DifferentTotalScore_ReturnsTrue()
    {
        var left = Composition(
            10.0,
            Group(Student("100001"), Student("100002")));
        var right = Composition(
            99.0,
            Group(Student("100001"), Student("100002")));

        Assert.Equal(left, right);
    }

    [Fact]
    public void Equals_SameNumbersDifferentStudentData_ReturnsTrue()
    {
        var left = Composition(
            Group(
                Student("100001", name: "Ada", wishes: ["100002"]),
                Student("100002", name: "Bob")));
        var right = Composition(
            Group(
                Student("100001", name: "Alice", wishes: ["100003"]),
                Student("100002", name: "Robert", wishes: ["100001"])));

        Assert.Equal(left, right);
    }

    #endregion

    #region Equality – different compositions

    [Fact]
    public void Equals_DifferentStudentNumbers_ReturnsFalse()
    {
        var left = Composition(
            Group(Student("100001"), Student("100002")));
        var right = Composition(
            Group(Student("100001"), Student("100003")));

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Equals_DifferentPartitioning_ReturnsFalse()
    {
        var left = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003"), Student("100004")));
        var right = Composition(
            Group(Student("100001"), Student("100003")),
            Group(Student("100002"), Student("100004")));

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var composition = Composition(
            Group(Student("100001"), Student("100002")));

        Assert.False(composition.Equals(null));
    }

    #endregion

    #region GetHashCode

    [Fact]
    public void GetHashCode_EqualCompositions_ReturnSameHash()
    {
        var left = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003"), Student("100004")));
        var right = Composition(
            Group(Student("100004"), Student("100003")),
            Group(Student("100002"), Student("100001")));

        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentScoreSameGroups_ReturnSameHash()
    {
        var left = Composition(
            1.0,
            Group(Student("100001"), Student("100002")));
        var right = Composition(
            50.0,
            Group(Student("100002"), Student("100001")));

        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameNumbersDifferentStudentData_ReturnSameHash()
    {
        var left = Composition(
            Group(Student("100001", name: "Ada", wishes: ["100002"]), Student("100002")));
        var right = Composition(
            Group(Student("100001", name: "Alice"), Student("100002", name: "Bob")));

        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void GetHashCode_CalledTwice_ReturnsSameValue()
    {
        var composition = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003")));

        Assert.Equal(composition.GetHashCode(), composition.GetHashCode());
    }

    [Fact]
    public void GetHashCode_EqualCompositions_WorkAsHashSetKey()
    {
        var first = Composition(
            Group(Student("100001"), Student("100002")),
            Group(Student("100003"), Student("100004")));
        var duplicate = Composition(
            Group(Student("100004"), Student("100003")),
            Group(Student("100002"), Student("100001")));

        var set = new HashSet<GroupComposition> { first };

        Assert.False(set.Add(duplicate));
        Assert.Single(set);
    }

    #endregion

    private static Student Student(
        string number,
        string? name = null,
        params string[] wishes) =>
        Logic.Models.Student.Create(
            number,
            wishes.Select(StudentNumber.Create).ToArray(),
            name);

    private static Group Group(params Student[] members) => new(members);

    private static GroupComposition Composition(params Group[] groups) =>
        new(groups);

    private static GroupComposition Composition(double score, params Group[] groups) =>
        new(groups, score);
}
