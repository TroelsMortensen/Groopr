namespace Logic.Models;

public class GroupSizeDistribution(IReadOnlyList<int> sizes)
{
    public IReadOnlyList<int> Sizes { get; } = sizes;

    public static GroupSizeDistribution Create(IReadOnlyList<int> sizes, int expectedTotalStudents)
    {
        var list = sizes?.ToList() ?? throw new ArgumentNullException(nameof(sizes));
        
        if (list.Count == 0)
            throw new ArgumentException("Group blueprint cannot be empty.");
            
        if (list.Sum() != expectedTotalStudents)
            throw new ArgumentException($"Group sizes sum to {list.Sum()}, but expected {expectedTotalStudents} students.");

        return new GroupSizeDistribution(list);
    }
}