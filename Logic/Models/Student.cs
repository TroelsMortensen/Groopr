namespace Logic.Models;

public class Student(
    string number,
    IReadOnlyList<StudentNumber> positiveWishes,
    string? name = null
    // more later
)
{
    public StudentNumber StudentNumber { get; } = StudentNumber.Create(number); // yikes, feels bad, will improve...?
    public string Number => StudentNumber.Value;

    public IReadOnlyList<StudentNumber> PositiveWishes { get; } = positiveWishes;
    public string? Name { get; } = name;

    public static Student Create(string number,
        IReadOnlyList<StudentNumber> positiveWishes,
        string? name = null)
    {
        EnsureNoSelfReference(number, positiveWishes);
        EnsureNoDuplicateWishes(number, positiveWishes);
        return new Student(number, positiveWishes, name);
    }

    private static void EnsureNoDuplicateWishes(string selfNumber, IReadOnlyList<StudentNumber> positiveWishes)
    {
        if (positiveWishes.Select(wish => wish.Value).Distinct().Count() != positiveWishes.Count)
        {
            throw new ArgumentException($"The student with number {selfNumber} cannot wish the same person twice.");
        }
    }

    private static void EnsureNoSelfReference(string selfNumber, IReadOnlyList<StudentNumber> wishes)
    {
        if (wishes.Any(wish => selfNumber.Equals(wish.Value)))
        {
            throw new ArgumentException($"The student with number {selfNumber} cannot be in their list of wishes.");
        }
    }

    public static Student Create(string number,
        IReadOnlyList<string> positiveWishes,
        string? name = null)
    {
        return Create(number, positiveWishes.Select(StudentNumber.Create).ToList(), name);
    }
}