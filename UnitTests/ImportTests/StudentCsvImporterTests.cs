using Logic.Import;
using Logic.Models;

namespace UnitTests.ImportTests;

public class StudentCsvImporterTests
{
    
    private readonly StudentCsvImporter _importer = new();

    #region Valid imports

    [Fact]
    public void Import_WithValidMultiRowCsv_ReturnsStudentListWithCorrectData()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,"100002, 100003"
            100002,Bob,100001
            100003,,
            """;

        StudentList studentList = Import(csv);

        Assert.Equal(3, studentList.Students.Count);
        Assert.Equal("100001", studentList.Students[0].Number);
        Assert.Equal("Alice", studentList.Students[0].Name);
        Assert.Equal(["100002", "100003"], studentList.Students[0].PositiveWishes.Select(w => w.Value));
        Assert.Equal("100002", studentList.Students[1].Number);
        Assert.Equal("Bob", studentList.Students[1].Name);
        Assert.Equal(["100001"], studentList.Students[1].PositiveWishes.Select(w => w.Value));
        Assert.Equal("100003", studentList.Students[2].Number);
        Assert.Null(studentList.Students[2].Name);
        Assert.Empty(studentList.Students[2].PositiveWishes);
        Assert.Empty(studentList.Students[0].PreviousGroupMembers);
        Assert.Empty(studentList.Students[1].PreviousGroupMembers);
        Assert.Empty(studentList.Students[2].PreviousGroupMembers);
    }

    [Fact]
    public void Import_WithCaseInsensitiveHeaders_ParsesSuccessfully()
    {
        const string csv = """
            studentnumber,name,positivewishes
            100001,Alice,
            """;

        StudentList studentList = Import(csv);

        Assert.Single(studentList.Students);
        Assert.Equal("100001", studentList.Students[0].Number);
    }

    #endregion

    #region Row-level validation

    [Fact]
    public void Import_WithInvalidStudentNumberOnRow_ThrowsWithRowNumber()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,
            abc,Bob,
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Equal(3, exception.RowNumber);
        Assert.Contains("Error on row 3:", exception.Message);
    }

    [Fact]
    public void Import_WithMissingStudentNumberOnRow_ThrowsWithRowNumber()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,
            ,Bob,
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Equal(3, exception.RowNumber);
        Assert.Contains("Student number is required.", exception.Message);
    }

    [Fact]
    public void Import_WithSelfWish_ThrowsWithRowNumber()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,100001
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Equal(2, exception.RowNumber);
        Assert.Contains("cannot be in their list of wishes", exception.Message);
    }

    [Fact]
    public void Import_WithDuplicateWishWithinRow_ThrowsWithRowNumber()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,
            100002,Bob,"100001, 100001"
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Equal(3, exception.RowNumber);
        Assert.Contains("cannot wish the same person twice", exception.Message);
    }

    #endregion

    #region List-level validation

    [Fact]
    public void Import_WithWishForNonExistentStudent_ThrowsStudentCsvImportException()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,999999
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Contains("999999", exception.Message);
        Assert.Contains("not in the list", exception.Message);
    }

    [Fact]
    public void Import_WithDuplicateStudentNumbers_ThrowsStudentCsvImportException()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,
            100001,Bob,
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Contains("Duplicate student numbers", exception.Message);
    }

    [Fact]
    public void Import_WithMoreThanTenWishes_ThrowsStudentCsvImportException()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,"100002,100003,100004,100005,100006,100007,100008,100009,100010,100011,100012"
            100002,,
            100003,,
            100004,,
            100005,,
            100006,,
            100007,,
            100008,,
            100009,,
            100010,,
            100011,,
            100012,,
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Contains("more than 10 positive wishes", exception.Message);
    }

    #endregion

    #region Header and file structure

    [Fact]
    public void Import_WithEmptyFile_ThrowsStudentCsvImportException()
    {
        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(string.Empty));

        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Import_WithHeaderOnly_ThrowsStudentCsvImportException()
    {
        const string csv = "StudentNumber,Name,PositiveWishes";

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Contains("no student rows", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Name,PositiveWishes")]
    [InlineData("StudentNumber,PositiveWishes")]
    [InlineData("StudentNumber,Name")]
    public void Import_WithMissingRequiredHeader_ThrowsBeforeProcessingRows(string headerLine)
    {
        var csv = $"{headerLine}\n100001,Alice,";

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Contains("Missing required column", exception.Message);
    }

    [Fact]
    public void Import_WithExtraHeaderColumn_ThrowsBeforeProcessingRows()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,Extra
            100001,Alice,
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Contains("Unexpected column", exception.Message);
    }

    #endregion

    #region Previous group members (optional column)

    [Fact]
    public void Import_WithPreviousGroupMembersColumn_ParsesPreviousGroupMembers()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,PreviousGroupMembers
            100001,Alice,"100002, 100003","100002, 100003"
            100002,Bob,100001,100001
            100003,,,
            """;

        StudentList studentList = Import(csv);

        Assert.Equal(["100002", "100003"], studentList.Students[0].PreviousGroupMembers.Select(m => m.Value));
        Assert.Equal(["100001"], studentList.Students[1].PreviousGroupMembers.Select(m => m.Value));
        Assert.Empty(studentList.Students[2].PreviousGroupMembers);
    }

    [Fact]
    public void Import_WithoutPreviousGroupMembersColumn_LeavesPreviousGroupMembersEmpty()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,100002
            100002,Bob,
            """;

        StudentList studentList = Import(csv);

        Assert.Empty(studentList.Students[0].PreviousGroupMembers);
        Assert.Empty(studentList.Students[1].PreviousGroupMembers);
    }

    [Fact]
    public void Import_WithSelfPreviousGroupMember_ThrowsWithRowNumber()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,PreviousGroupMembers
            100001,Alice,,100001
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Equal(2, exception.RowNumber);
        Assert.Contains("cannot be in their list of wishes", exception.Message);
    }

    [Fact]
    public void Import_WithPreviousGroupMemberForNonExistentStudent_ThrowsStudentCsvImportException()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,PreviousGroupMembers
            100001,Alice,,999999
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Contains("999999", exception.Message);
        Assert.Contains("previous group member", exception.Message);
    }

    #endregion

    #region Negative wishes (optional column)

    [Fact]
    public void Import_WithNegativeWishesColumn_ParsesNegativeWishes()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,NegativeWishes
            100001,Alice,,"100002, 100003"
            100002,Bob,,100001
            100003,,,
            """;

        StudentList studentList = Import(csv);

        Assert.Equal(["100002", "100003"], studentList.Students[0].NegativeWishes.Select(m => m.Value));
        Assert.Equal(["100001"], studentList.Students[1].NegativeWishes.Select(m => m.Value));
        Assert.Empty(studentList.Students[2].NegativeWishes);
    }

    [Fact]
    public void Import_WithoutNegativeWishesColumn_LeavesNegativeWishesEmpty()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes
            100001,Alice,100002
            100002,Bob,
            """;

        StudentList studentList = Import(csv);

        Assert.Empty(studentList.Students[0].NegativeWishes);
        Assert.Empty(studentList.Students[1].NegativeWishes);
    }

    [Fact]
    public void Import_WithSelfNegativeWish_ThrowsWithRowNumber()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,NegativeWishes
            100001,Alice,,100001
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Equal(2, exception.RowNumber);
        Assert.Contains("cannot be in their list of wishes", exception.Message);
    }

    [Fact]
    public void Import_WithDuplicateNegativeWishWithinRow_ThrowsWithRowNumber()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,NegativeWishes
            100001,Alice,,"100002, 100002"
            100002,Bob,,
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Equal(2, exception.RowNumber);
        Assert.Contains("cannot wish the same person twice", exception.Message);
    }

    [Fact]
    public void Import_WithPositiveAndNegativeWishOverlap_ThrowsWithRowNumber()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,NegativeWishes
            100001,Alice,100002,100002
            100002,Bob,,
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Equal(2, exception.RowNumber);
        Assert.Contains("positive and negative", exception.Message);
    }

    [Fact]
    public void Import_WithNegativeWishForNonExistentStudent_ThrowsStudentCsvImportException()
    {
        const string csv = """
            StudentNumber,Name,PositiveWishes,NegativeWishes
            100001,Alice,,999999
            """;

        StudentCsvImportException exception = Assert.Throws<StudentCsvImportException>(() => Import(csv));

        Assert.Contains("999999", exception.Message);
        Assert.Contains("negative wish", exception.Message);
    }

    #endregion

    #region Helpers

    private StudentList Import(string csv) =>
        _importer.Import(new StringReader(csv));

    #endregion
}
