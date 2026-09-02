using Logic.Import;

namespace UnitTests.ImportTests;

public class StudentCsvTemplateTests
{
    [Fact]
    public void Header_ReturnsExpectedColumnNames()
    {
        Assert.Equal("StudentNumber,Name,PositiveWishes,PreviousGroupMembers", StudentCsvTemplate.Header);
    }

    [Fact]
    public void GetContent_IncludesHeaderAndExampleRows()
    {
        var content = StudentCsvTemplate.GetContent();

        Assert.StartsWith(StudentCsvTemplate.Header, content);
        Assert.Contains("100001,Alice,\"100002, 100003\",\"100002, 100003\"", content);
        Assert.Contains("100002,Bob,100001,100001", content);
        Assert.Contains("100003,,,", content);
    }

    [Fact]
    public void GetContent_ExampleRowsUseValidSixDigitStudentNumbers()
    {
        var content = StudentCsvTemplate.GetContent();
        var lines = content.Split(Environment.NewLine);

        Assert.Equal(4, lines.Length);
        Assert.Matches(@"^\d{6},", lines[1]);
        Assert.Matches(@"^\d{6},", lines[2]);
        Assert.Matches(@"^\d{6},", lines[3]);
    }
}
