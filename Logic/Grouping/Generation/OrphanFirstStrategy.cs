using Logic.Models;

namespace Logic.Grouping.Generation;

public class OrphanFirstStrategy : IGroupCompositionProducer
{
    private readonly IReadOnlyList<Student> students;
    private readonly IReadOnlyList<int> groupSizes;
    private readonly Dictionary<string, int> incomingWishCounts;

    // This is used for unit testing — breaks ties among equal incoming-wish counts.
    internal Func<IReadOnlyList<Student>, IReadOnlyList<Student>> Shuffle { get; set; } =
        (students) => students.OrderBy(_ => Random.Shared.Next()).ToList();

    // This is used for unit testing
    internal Func<IReadOnlyList<Student>, Student> PickWish { get; set; } =
        candidates => candidates[Random.Shared.Next(candidates.Count)];

    public OrphanFirstStrategy(StudentList studentList, GroupSizeDistribution groupSizes)
    {
        if (studentList.Students.Count != groupSizes.Sizes.Sum())
        {
            throw new ArgumentException($"The sum of group sizes must equal the number of students ({studentList.Students.Count}).");
        }

        students = studentList.Students;
        this.groupSizes = groupSizes.Sizes;
        incomingWishCounts = CountIncomingWishes(students);
    }

    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Shuffle first so OrderBy (stable) randomizes among equal orphan scores.
            var unassigned = Shuffle(students)
                .OrderBy(student => incomingWishCounts[student.Number])
                .ToList();
            var unassignedByNumber = unassigned.ToDictionary(student => student.Number);
            var groups = new List<Group>();

            foreach (var size in groupSizes)
            {
                groups.Add(BuildGroup(size, unassigned, unassignedByNumber));
            }

            yield return new GroupComposition(groups, 0);
        }
    }

    private Group BuildGroup(
        int targetSize,
        List<Student> unassigned,
        Dictionary<string, Student> unassignedByNumber)
    {
        var currentGroup = new List<Student>(targetSize);

        Student seed = PopFirst(unassigned, unassignedByNumber);
        currentGroup.Add(seed);

        while (currentGroup.Count < targetSize)
        {
            Student? next = TryPickWishCandidate(currentGroup, unassignedByNumber);
            if (next is null)
            {
                next = PopFirst(unassigned, unassignedByNumber);
            }
            else
            {
                RemoveUnassigned(next, unassigned, unassignedByNumber);
            }

            currentGroup.Add(next);
        }

        return new Group(currentGroup);
    }

    private Student? TryPickWishCandidate(
        List<Student> currentGroup,
        Dictionary<string, Student> unassignedByNumber)
    {
        foreach (Student member in currentGroup)
        {
            List<Student> available = [];
            foreach (StudentNumber wish in member.PositiveWishes)
            {
                if (unassignedByNumber.TryGetValue(wish.Value, out Student? candidate))
                {
                    available.Add(candidate);
                }
            }

            if (available.Count > 0)
            {
                return PickWish(available);
            }
        }

        return null;
    }

    private static Dictionary<string, int> CountIncomingWishes(IReadOnlyList<Student> allStudents)
    {
        Dictionary<string, int> counts = allStudents.ToDictionary(student => student.Number, _ => 0);

        foreach (Student student in allStudents)
        {
            foreach (StudentNumber wish in student.PositiveWishes)
            {
                if (counts.ContainsKey(wish.Value))
                {
                    counts[wish.Value]++;
                }
            }
        }

        return counts;
    }

    private static Student PopFirst(List<Student> unassigned, Dictionary<string, Student> unassignedByNumber)
    {
        Student student = unassigned[0];
        RemoveUnassigned(student, unassigned, unassignedByNumber);
        return student;
    }

    private static void RemoveUnassigned(
        Student student,
        List<Student> unassigned,
        Dictionary<string, Student> unassignedByNumber)
    {
        unassigned.Remove(student);
        unassignedByNumber.Remove(student.Number);
    }
}
