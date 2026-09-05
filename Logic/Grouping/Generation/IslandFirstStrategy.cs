using Logic.Models;

namespace Logic.Grouping.Generation;

public class IslandFirstStrategy : IGroupCompositionProducer
{
    private readonly IReadOnlyList<Student> students;
    private readonly IReadOnlyList<int> groupSizes;

    // This is used for unit testing
    internal Func<IReadOnlyList<Student>, IReadOnlyList<Student>> Shuffle { get; set; } =
        (students) => students.OrderBy(_ => Random.Shared.Next()).ToList();

    // This is used for unit testing
    internal Func<IReadOnlyList<Student>, Student> PickWish { get; set; } =
        candidates => candidates[Random.Shared.Next(candidates.Count)];

    public IslandFirstStrategy(StudentList studentList, GroupSizeDistribution groupSizes)
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
            var islands = FindConnectedComponents(unassigned)
                .OrderByDescending(island => island.Count)
                .ToList();
            var groups = new List<Group>();

            foreach (var size in groupSizes)
            {
                groups.Add(BuildGroup(size, unassigned, unassignedByNumber, islands));
            }

            yield return new GroupComposition(groups, 0);
        }
    }

    private Group BuildGroup(
        int targetSize,
        List<Student> unassigned,
        Dictionary<string, Student> unassignedByNumber,
        List<List<Student>> islands)
    {
        var currentGroup = new List<Student>(targetSize);

        TrySeedWithFittingIsland(currentGroup, unassigned, unassignedByNumber, islands, targetSize);

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

    private static void TrySeedWithFittingIsland(
        List<Student> currentGroup,
        List<Student> unassigned,
        Dictionary<string, Student> unassignedByNumber,
        List<List<Student>> islands,
        int targetSize)
    {
        for (int index = 0; index < islands.Count; index++)
        {
            List<Student> island = islands[index];
            if (island.Count > targetSize)
            {
                continue;
            }

            if (!island.All(member => unassignedByNumber.ContainsKey(member.Number)))
            {
                continue;
            }

            foreach (Student member in island)
            {
                currentGroup.Add(member);
                RemoveUnassigned(member, unassigned, unassignedByNumber);
            }

            islands.RemoveAt(index);
            return;
        }
    }

    private static List<List<Student>> FindConnectedComponents(List<Student> unassigned)
    {
        Dictionary<string, Student> byNumber = unassigned.ToDictionary(student => student.Number);
        Dictionary<string, List<Student>> adjacency = unassigned.ToDictionary(
            student => student.Number,
            _ => new List<Student>());

        foreach (Student student in unassigned)
        {
            foreach (StudentNumber wish in student.PositiveWishes)
            {
                if (!byNumber.TryGetValue(wish.Value, out Student? other))
                {
                    continue;
                }

                adjacency[student.Number].Add(other);
                adjacency[other.Number].Add(student);
            }
        }

        HashSet<string> visited = [];
        List<List<Student>> components = [];

        foreach (Student start in unassigned)
        {
            if (visited.Contains(start.Number))
            {
                continue;
            }

            List<Student> component = [];
            var stack = new Stack<Student>();
            stack.Push(start);
            visited.Add(start.Number);

            while (stack.Count > 0)
            {
                Student current = stack.Pop();
                component.Add(current);

                foreach (Student neighbor in adjacency[current.Number])
                {
                    if (visited.Add(neighbor.Number))
                    {
                        stack.Push(neighbor);
                    }
                }
            }

            components.Add(component);
        }

        return components;
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
