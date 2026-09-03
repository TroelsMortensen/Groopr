namespace Logic.Models;

public class Student(
    string number,
    IReadOnlyList<StudentNumber> positiveWishes,
    string? name = null,
    IReadOnlyList<StudentNumber>? previousGroupMembers = null,
    IReadOnlyList<StudentNumber>? negativeWishes = null
)
{
    public StudentNumber StudentNumber { get; } = StudentNumber.Create(number); // yikes, feels bad, will improve...?
    public string Number => StudentNumber.Value;

    public IReadOnlyList<StudentNumber> PositiveWishes { get; } = positiveWishes;
    public string? Name { get; } = name;
    public IReadOnlyList<StudentNumber> PreviousGroupMembers { get; } =previousGroupMembers ?? [];
    public IReadOnlyList<StudentNumber> NegativeWishes { get; } = negativeWishes ?? [];

    private Student() : this("", [])
    {}
    
    public static Student Create(string number,
        IReadOnlyList<StudentNumber> positiveWishes,
        string? name = null,
        IReadOnlyList<StudentNumber>? previousGroupMembers = null,
        IReadOnlyList<StudentNumber>? negativeWishes = null)
    {
        EnsureNoSelfReference(number, positiveWishes);
        EnsureNoSelfReference(number, previousGroupMembers);
        EnsureNoSelfReference(number, negativeWishes);
        EnsureNoDuplicates(number, positiveWishes);
        EnsureNoDuplicates(number, negativeWishes);
        EnsureNoDuplicates(number, previousGroupMembers);
        return new Student(number, positiveWishes, name, previousGroupMembers, negativeWishes);
    }

    private static void EnsureNoDuplicates(string selfNumber, IReadOnlyList<StudentNumber>? positiveWishes)
    {
        if (positiveWishes == null) return;
        if (positiveWishes.Select(wish => wish.Value).Distinct().Count() != positiveWishes.Count)
        {
            throw new ArgumentException($"The student with number {selfNumber} cannot wish the same person twice.");
        }
    }

    private static void EnsureNoSelfReference(string selfNumber, IReadOnlyList<StudentNumber>? wishes)
    {
        if (wishes == null) return;
        if (wishes.Any(wish => selfNumber.Equals(wish.Value)))
        {
            throw new ArgumentException($"The student with number {selfNumber} cannot be in their list of wishes.");
        }
    }

    public static Student Create(string number,
        IReadOnlyList<string> positiveWishes,
        string? name = null,
        IReadOnlyList<string>? previousGroupMembers = null)
    {
        return Create(
            number,
            positiveWishes.Select(StudentNumber.Create).ToList(),
            name,
            previousGroupMembers?.Select(StudentNumber.Create).ToList());
    }
}