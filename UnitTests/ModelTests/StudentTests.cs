using Logic.Models;

namespace UnitTests.ModelTests;

public class StudentTests
{
    private static readonly IReadOnlyList<StudentNumber> EmptyWishes = [];

    #region Valid student numbers, // TODO should be in a StudentNumber test class

    [Theory]
    [InlineData("000000")]
    [InlineData("000001")]
    [InlineData("123456")]
    [InlineData("999999")]
    public void Create_WithExactlySixDigits_ReturnsStudentWithNumber(string number)
    {
        Student student = Student.Create(number, EmptyWishes);

        Assert.Equal(number, student.Number);
    }

    [Fact]
    public void Create_WithValidNumberAndOptionalFields_ReturnsStudentWithAllProperties()
    {
        IReadOnlyList<string> wishNumbers = ["100001", "100002"];
        const string name = "Ada Lovelace";

        Student student = Student.Create("100003", wishNumbers.Select(StudentNumber.Create).ToArray(), name);

        Assert.Equal("100003", student.Number);
        Assert.Equal(wishNumbers, student.PositiveWishes.Select(wish => wish.Value));
        Assert.Equal(name, student.Name);
    }

    [Fact]
    public void Create_WithValidNumberAndNullName_ReturnsStudentWithNullName()
    {
        Student student = Student.Create("100004", EmptyWishes);

        Assert.Null(student.Name);
    }

    #endregion

    #region Null, empty, or whitespace numbers

    [Fact]
    public void Create_WithNullNumber_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => Student.Create(null!, EmptyWishes));

        Assert.Equal("Student number cannot be null or empty", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Create_WithNullOrWhitespaceNumber_ThrowsArgumentNullException(string number)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => Student.Create(number, EmptyWishes));

        Assert.Equal("Student number cannot be null or empty", exception.ParamName);
    }

    #endregion

    #region Length must be exactly six characters

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("12345")]
    public void Create_WithFewerThanSixCharacters_ThrowsArgumentException(string number)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Student.Create(number, EmptyWishes));

        Assert.Equal("Student number must contain exactly 6 digits", exception.Message);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("12345678")]
    [InlineData("1234567890")]
    public void Create_WithMoreThanSixCharacters_ThrowsArgumentException(string number)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Student.Create(number, EmptyWishes));

        Assert.Equal("Student number must contain exactly 6 digits", exception.Message);
    }

    #endregion

    #region Digits only

    [Theory]
    [InlineData("12345a")]
    [InlineData("a23456")]
    [InlineData("12a456")]
    [InlineData("12-456")]
    [InlineData("12.456")]
    [InlineData("12_456")]
    [InlineData("12 456")]
    [InlineData("12345 ")]
    [InlineData(" 12345")]
    public void Create_WithNonDigitCharacters_ThrowsArgumentException(string number)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Student.Create(number, EmptyWishes));

        Assert.Equal("Student number must contain exactly 6 digits", exception.Message);
    }

    [Theory]
    [InlineData("ABCDEF")]
    [InlineData("abc123")]
    [InlineData("!!!!!!")]
    public void Create_WithSixNonDigitCharacters_ThrowsArgumentException(string number)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Student.Create(number, EmptyWishes));

        Assert.Equal("Student number must contain exactly 6 digits", exception.Message);
    }

    #endregion

    #region Self-reference

    [Fact]
    public void Create_WithSelfWish_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Student.Create("100001", Wishes("100001")));

        Assert.Contains("100001", exception.Message);
    }

    [Fact]
    public void Create_WithSelfWishAmongValidWishes_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Student.Create("100001", Wishes("100002", "100001", "100003")));

        Assert.Contains("100001", exception.Message);
    }

    #endregion

    #region Duplicate wishes

    [Fact]
    public void Create_WithDuplicateWishes_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Student.Create("100001", Wishes("100002", "100002")));

        Assert.Contains("100001", exception.Message);
    }

    [Fact]
    public void Create_WithDuplicateWishesAmongValidOnes_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Student.Create("100001", Wishes("100002", "100003", "100002")));

        Assert.Contains("100001", exception.Message);
    }

    [Fact]
    public void Create_WithDuplicateAtStart_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Student.Create("100001", Wishes("100002", "100002", "100003")));

        Assert.Contains("100001", exception.Message);
    }

    [Fact]
    public void Create_WithDuplicateInMiddle_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Student.Create("100001", Wishes("100002", "100003", "100002")));

        Assert.Contains("100001", exception.Message);
    }

    [Fact]
    public void Create_WithDuplicateAtEnd_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Student.Create("100001", Wishes("100003", "100002", "100002")));

        Assert.Contains("100001", exception.Message);
    }

    #endregion

    #region Helpers

    private static IReadOnlyList<StudentNumber> Wishes(params string[] numbers)
        => numbers.Select(StudentNumber.Create).ToArray();

    #endregion
}
