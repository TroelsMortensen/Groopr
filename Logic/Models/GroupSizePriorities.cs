namespace Logic.Models;

public class GroupSizePriorities(int[] sizes)
{
    public int[] Sizes { get; } = sizes;

    public static GroupSizePriorities Create(int[] sizes)
    {
        if (sizes.Min() < 1)
        {
            throw new Exception("The minimum priorities must be greater than or equal to 1.");
        }
        return new GroupSizePriorities(sizes);
    }
}
