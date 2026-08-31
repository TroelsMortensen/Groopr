namespace Logic.Models;

public class StudentNumber(string value)
{
    public string Value { get; } = value;

    public static StudentNumber Create(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentNullException("Student number cannot be null or empty");
        }

        if (number.Length != 6)
        {
            throw new ArgumentException("Student number must contain exactly 6 digits");
        }

        if (!number.All(char.IsDigit))
        {
            throw new ArgumentException("Student number must contain exactly 6 digits");
        }

        return new StudentNumber(number);
    }
}