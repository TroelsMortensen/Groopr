using Logic.Import;

namespace UnitTests.ImportTests;

public class StudentCsvTemplateTests
{
    [Fact]
    public void Header_ReturnsExpectedColumnNames()
    {
        Assert.Equal("StudentNumber,Name,PositiveWishes", StudentCsvTemplate.Header);
    }

    [Fact]
    public void GetContent_IncludesHeaderAndExampleRow()
    {
        var content = StudentCsvTemplate.GetContent();

        Assert.StartsWith(StudentCsvTemplate.Header, content);
        Assert.Contains("123456,John Doe,\"654321, 789654\"", content);
    }

    [Fact]
    public void GetContent_ExampleRowUsesValidSixDigitStudentNumbers()
    {
        var content = StudentCsvTemplate.GetContent();
        var lines = content.Split(Environment.NewLine);

        Assert.Equal(2, lines.Length);
        Assert.Matches(@"^\d{6},", lines[1]);
    }
}
