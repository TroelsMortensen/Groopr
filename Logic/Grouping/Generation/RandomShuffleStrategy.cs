using Logic.Models;

namespace Logic.Grouping.Generation;

public class RandomShuffleStrategy : IGroupCompositionProducer
{
    private readonly IReadOnlyList<Student> students;
    private readonly IReadOnlyList<int> groupSizes;

    // This is used for unit testing
    internal Func<IReadOnlyList<Student>, IReadOnlyList<Student>> Shuffle { get; set; } =
        (students) => students.OrderBy(_ => Random.Shared.Next()).ToList();

    public RandomShuffleStrategy(StudentList studentList, GroupSizeDistribution groupSizes)
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
            var shuffled = Shuffle(students);

            var groups = new List<Group>();
            int index = 0;

            foreach (var size in groupSizes)
            {
                groups.Add(new Group(shuffled.Skip(index).Take(size).ToList()));
                index += size;
            }

            yield return new GroupComposition(groups, 0);
        }
    }
}