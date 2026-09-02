namespace Logic.Models;

public class StudentList(List<Student> students)
{
    public List<Student> Students { get; } = students;

    public static StudentList Create(List<Student> students)
    {
        if (students is null || students.Count == 0)
        {
            throw new ArgumentException($"{nameof(students)} must not be null or empty");
        }

        ValidateCrossReferences(students);
        EnsureNoDuplicateStudentNumbers(students);
        NoMoreThanTenWishes(students);
        return new StudentList(students);
    }

    private static void NoMoreThanTenWishes(List<Student> list) =>
        list.ForEach(student =>
        {
            if (student.PositiveWishes.Count > 10)
            {
                throw new ArgumentException($"Student with number {student.Number} has more than 10 positive wishes");
            }
        });

    private static void ValidateCrossReferences(List<Student> list)
    {
        HashSet<string> studentNumbers = list.Select(s => s.Number).ToHashSet();
        List<string> errors = [];

        foreach (Student student in list)
        {
            foreach (StudentNumber wish in student.PositiveWishes)
            {
                if (!studentNumbers.Contains(wish.Value))
                {
                    errors.Add(
                        $"The student with number {student.Number} has a wish for {wish.Value}, who is not in the list.");
                }
            }

            foreach (StudentNumber previousGroupMember in student.PreviousGroupMembers)
            {
                if (!studentNumbers.Contains(previousGroupMember.Value))
                {
                    errors.Add(
                        $"The student with number {student.Number} has a previous group member {previousGroupMember.Value}, who is not in the list.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, errors));
        }
    }

    private static void EnsureNoDuplicateStudentNumbers(List<Student> list)
    {
        string[] duplicates = list
            .GroupBy(student => student.Number)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new ArgumentException(
                $"Duplicate student numbers found in the list: {string.Join(", ", duplicates)}.");
        }
    }
}