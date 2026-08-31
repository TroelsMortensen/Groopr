using Logic.Models;

namespace UnitTests.ModelTests;

public class StudentListTests
{
    #region Valid positive wish references

    [Fact]
    public void Create_WithSingleStudentAndEmptyWishes_ReturnsStudentList()
    {
        StudentList studentList = CreateList(MakeStudent("100001"));

        Assert.Single(studentList.Students);
        Assert.Equal("100001", studentList.Students[0].Number);
    }

    [Fact]
    public void Create_WithMultipleStudentsAndEmptyWishes_ReturnsStudentList()
    {
        StudentList studentList = CreateList(
            MakeStudent("100001"),
            MakeStudent("100002"),
            MakeStudent("100003"));

        Assert.Equal(3, studentList.Students.Count);
        Assert.Equal(["100001", "100002", "100003"], studentList.Students.Select(s => s.Number));
    }

    [Fact]
    public void Create_WithValidCrossReferences_ReturnsStudentList()
    {
        StudentList studentList = CreateList(
            MakeStudent("100001", "100002"),
            MakeStudent("100002", "100001"));

        Assert.Equal(2, studentList.Students.Count);
    }

    [Fact]
    public void Create_WithValidOneWayWishes_ReturnsStudentList()
    {
        StudentList studentList = CreateList(
            MakeStudent("100001", "100002"),
            MakeStudent("100002"));

        Assert.Equal(2, studentList.Students.Count);
    }

    [Fact]
    public void Create_WithTypicalWishCount_ReturnsStudentList()
    {
        StudentList studentList = CreateList(
            MakeStudent("100001", "100002", "100003", "100004"),
            MakeStudent("100002"),
            MakeStudent("100003"),
            MakeStudent("100004"),
            MakeStudent("100005"));

        Assert.Equal(5, studentList.Students.Count);
        Assert.Equal(3, studentList.Students[0].PositiveWishes.Count);
    }

    [Fact]
    public void Create_WithMaxAllowedWishes_ReturnsStudentList()
    {
        Student[] roster =
        [
            MakeStudent("100001", "100002", "100003", "100004", "100005", "100006", "100007", "100008", "100009", "100010", "100011"),
            MakeStudent("100002"),
            MakeStudent("100003"),
            MakeStudent("100004"),
            MakeStudent("100005"),
            MakeStudent("100006"),
            MakeStudent("100007"),
            MakeStudent("100008"),
            MakeStudent("100009"),
            MakeStudent("100010"),
            MakeStudent("100011"),
        ];

        StudentList studentList = CreateList(roster);

        Assert.Equal(11, studentList.Students.Count);
        Assert.Equal(10, studentList.Students[0].PositiveWishes.Count);
    }

    [Fact]
    public void Create_WithTenWishes_ReturnsStudentList()
    {
        Student[] roster =
        [
            MakeStudent("100001", "100002", "100003", "100004", "100005", "100006", "100007", "100008", "100009", "100010", "100011"),
            MakeStudent("100002"),
            MakeStudent("100003"),
            MakeStudent("100004"),
            MakeStudent("100005"),
            MakeStudent("100006"),
            MakeStudent("100007"),
            MakeStudent("100008"),
            MakeStudent("100009"),
            MakeStudent("100010"),
            MakeStudent("100011"),
        ];

        StudentList studentList = CreateList(roster);

        Assert.Equal(10, studentList.Students[0].PositiveWishes.Count);
    }

    #endregion

    #region Duplicate student numbers

    [Fact]
    public void Create_WithDuplicateStudentNumbers_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateList(
                MakeStudent("100001"),
                MakeStudent("100001")));

        Assert.Contains("100001", exception.Message);
    }

    [Fact]
    public void Create_WithDuplicateStudentNumbersAmongOthers_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateList(
                MakeStudent("100001"),
                MakeStudent("100002"),
                MakeStudent("100003"),
                MakeStudent("100002")));

        Assert.Contains("100002", exception.Message);
    }

    #endregion

    #region Unknown student reference

    [Fact]
    public void Create_WithWishForStudentNotInList_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateList(
                MakeStudent("100001", "999999"),
                MakeStudent("100002"),
                MakeStudent("100003")));

        Assert.Contains("100001", exception.Message);
    }

    [Fact]
    public void Create_WithMixedValidAndInvalidWishes_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateList(
                MakeStudent("100001", "100002", "999999"),
                MakeStudent("100002"),
                MakeStudent("100003")));

        Assert.Contains("100001", exception.Message);
    }

    [Fact]
    public void Create_WithSecondStudentInvalidWish_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CreateList(
                MakeStudent("100001", "100002"),
                MakeStudent("100002", "999999"),
                MakeStudent("100003")));

        Assert.Contains("100002", exception.Message);
    }

    #endregion

    #region Max wishes per student

    [Fact]
    public void Create_WithElevenWishes_ThrowsArgumentException()
    {
        Student[] roster =
        [
            MakeStudent("100001", "100002", "100003", "100004", "100005", "100006", "100007", "100008", "100009", "100010", "100011", "100012"),
            MakeStudent("100002"),
            MakeStudent("100003"),
            MakeStudent("100004"),
            MakeStudent("100005"),
            MakeStudent("100006"),
            MakeStudent("100007"),
            MakeStudent("100008"),
            MakeStudent("100009"),
            MakeStudent("100010"),
            MakeStudent("100011"),
            MakeStudent("100012"),
        ];

        ArgumentException exception = Assert.Throws<ArgumentException>(() => CreateList(roster));

        Assert.Contains("100001", exception.Message);
    }

    #endregion

    #region Helpers

    private static IReadOnlyList<StudentNumber> Wishes(params string[] numbers)
        => numbers.Select(StudentNumber.Create).ToArray();

    private static Student MakeStudent(string number, params string[] positiveWishes)
        => Logic.Models.Student.Create(number, Wishes(positiveWishes));

    private static StudentList CreateList(params Student[] students)
        => StudentList.Create(students.ToList());

    #endregion
}
