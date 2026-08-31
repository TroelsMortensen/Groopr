using Logic.Import;

namespace UnitTests.ImportTests;

public class StudentCsvImportExceptionTests
{
    [Fact]
    public void Constructor_WithoutRowNumber_StoresMessage()
    {
        var exception = new StudentCsvImportException("Something went wrong.");

        Assert.Equal("Something went wrong.", exception.Message);
        Assert.Null(exception.RowNumber);
    }

    [Fact]
    public void Constructor_WithRowNumber_FormatsMessageWithRowPrefix()
    {
        var exception = new StudentCsvImportException("Student number is required.", rowNumber: 4);

        Assert.Equal("Error on row 4: Student number is required.", exception.Message);
        Assert.Equal(4, exception.RowNumber);
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new StudentCsvImportException("outer", rowNumber: 2, innerException: inner);

        Assert.Same(inner, exception.InnerException);
    }
}
