using Logic.Models;

namespace Logic.Grouping.Generation;

public class MutualPairFirstStrategy : IGroupCompositionProducer
{
    private readonly IReadOnlyList<Student> students;
    private readonly IReadOnlyList<int> groupSizes;

    // This is used for unit testing
    internal Func<IReadOnlyList<Student>, IReadOnlyList<Student>> Shuffle { get; set; } =
        (students) => students.OrderBy(_ => Random.Shared.Next()).ToList();

    // This is used for unit testing
    internal Func<IReadOnlyList<Student>, Student> PickWish { get; set; } =
        candidates => candidates[Random.Shared.Next(candidates.Count)];

    public MutualPairFirstStrategy(StudentList studentList, GroupSizeDistribution groupSizes)
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
            var mutualPairs = FindMutualPairs(unassigned);
            var groups = new List<Group>();

            foreach (var size in groupSizes)
            {
                groups.Add(BuildGroup(size, unassigned, unassignedByNumber, mutualPairs));
            }

            yield return new GroupComposition(groups, 0);
        }
    }

    private Group BuildGroup(
        int targetSize,
        List<Student> unassigned,
        Dictionary<string, Student> unassignedByNumber,
        List<(Student StudentA, Student StudentB)> mutualPairs)
    {
        var currentGroup = new List<Student>(targetSize);

        if (targetSize >= 2)
        {
            TrySeedWithMutualPair(currentGroup, unassigned, unassignedByNumber, mutualPairs);
        }

        if (currentGroup.Count == 0)
        {
            Student seed = PopFirst(unassigned, unassignedByNumber);
            currentGroup.Add(seed);
        }

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

    private static void TrySeedWithMutualPair(
        List<Student> currentGroup,
        List<Student> unassigned,
        Dictionary<string, Student> unassignedByNumber,
        List<(Student StudentA, Student StudentB)> mutualPairs)
    {
        while (mutualPairs.Count > 0)
        {
            (Student studentA, Student studentB) = mutualPairs[0];
            mutualPairs.RemoveAt(0);

            if (!unassignedByNumber.ContainsKey(studentA.Number)
                || !unassignedByNumber.ContainsKey(studentB.Number))
            {
                continue;
            }

            currentGroup.Add(studentA);
            currentGroup.Add(studentB);
            RemoveUnassigned(studentA, unassigned, unassignedByNumber);
            RemoveUnassigned(studentB, unassigned, unassignedByNumber);
            return;
        }
    }

    private static List<(Student StudentA, Student StudentB)> FindMutualPairs(List<Student> unassigned)
    {
        Dictionary<string, Student> byNumber = unassigned.ToDictionary(student => student.Number);
        HashSet<string> paired = [];
        List<(Student StudentA, Student StudentB)> pairs = [];

        foreach (Student student in unassigned)
        {
            if (paired.Contains(student.Number))
            {
                continue;
            }

            foreach (StudentNumber wish in student.PositiveWishes)
            {
                if (!byNumber.TryGetValue(wish.Value, out Student? other))
                {
                    continue;
                }

                if (paired.Contains(other.Number))
                {
                    continue;
                }

                if (!other.PositiveWishes.Any(otherWish => otherWish.Value == student.Number))
                {
                    continue;
                }

                pairs.Add((student, other));
                paired.Add(student.Number);
                paired.Add(other.Number);
                break;
            }
        }

        return pairs;
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
