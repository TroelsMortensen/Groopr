using Logic.Models;

namespace Logic.Grouping.Generation;

public class DepthFirstGreedyStrategy : IGroupCompositionProducer
{
    private readonly IReadOnlyList<Student> students;
    private readonly IReadOnlyList<int> groupSizes;

    // This is used for unit testing
    internal Func<IReadOnlyList<Student>, IReadOnlyList<Student>> Shuffle { get; set; } =
        (students) => students.OrderBy(_ => Random.Shared.Next()).ToList();

    // This is used for unit testing
    internal Func<IReadOnlyList<Student>, Student> PickWish { get; set; } =
        candidates => candidates[Random.Shared.Next(candidates.Count)];

    public DepthFirstGreedyStrategy(StudentList studentList, GroupSizeDistribution groupSizes)
    {
        if (studentList.Students.Count != groupSizes.Sizes.Sum())
        {
            throw new ArgumentException($"The sum of group sizes must equal the number of students ({studentList.Students.Count}).");
        }

        students = studentList.Students;
        this.groupSizes = groupSizes.Sizes;
    }

    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var unassigned = Shuffle(students).ToList();
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

        Student tip = PopFirst(unassigned, unassignedByNumber);
        currentGroup.Add(tip);

        while (currentGroup.Count < targetSize)
        {
            Student? next = TryPickWishCandidate(tip, unassignedByNumber);
            if (next is null)
            {
                next = PopFirst(unassigned, unassignedByNumber);
            }
            else
            {
                RemoveUnassigned(next, unassigned, unassignedByNumber);
            }

            currentGroup.Add(next);
            tip = next;
        }

        return new Group(currentGroup);
    }

    private Student? TryPickWishCandidate(
        Student tip,
        Dictionary<string, Student> unassignedByNumber)
    {
        List<Student> available = [];
        foreach (StudentNumber wish in tip.PositiveWishes)
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

        return null;
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
