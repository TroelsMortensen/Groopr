using System.Collections.Immutable;
using Logic.Models;

namespace Logic.GroupSizing;

public static class GroupSizesCalculator
{
    public static GroupSizeDistribution DetermineGroupSizes(int numberOfStudents, GroupSizePriorities groupSizePriorities)
    {
        VerifyValidInput(numberOfStudents, groupSizePriorities);

        var sizes = StartGroupSizeCalculation(numberOfStudents, groupSizePriorities.Sizes);

        return GroupSizeDistribution.Create(sizes, numberOfStudents);
    }

    private static void VerifyValidInput(int numberOfStudents, GroupSizePriorities groupSizePriorities)
    {
        if (numberOfStudents < groupSizePriorities.Sizes.Min())
        {
            throw new Exception($"Not enough students to form a group of the desired sizes: {string.Join(", ", groupSizePriorities)}.");
        }
    }

    private static IReadOnlyList<int> StartGroupSizeCalculation(int numberOfStudents, int[] groupSizePriorities)
    {

        for (int i = 0; i < groupSizePriorities.Length; i++)
        {

            // Progressively allow more priority options if stricter ones fail
            var allowedPriorities = groupSizePriorities.Take(i + 1).ToArray();
            var result = RecursivelyFigureOutGroupSizes(numberOfStudents, allowedPriorities, []);

            if (result is not null)
            {
                return result!;
            }
        }

        throw new Exception("No group sizes are available.");
}

    private static IReadOnlyList<int>? RecursivelyFigureOutGroupSizes(
        int remainingStudents, 
        int[] allowedPriorities, 
        IReadOnlyList<int> currentPath)
    {
        if (remainingStudents == 0)
        {
            return currentPath;
        }

        int minGroupSize = allowedPriorities.Min();

        foreach (int size in allowedPriorities)
        {
            int nextRemaining = remainingStudents - size;

            // Either we hit 0 exactly or we still have enough students left for at least the smallest allowed group size
            if (nextRemaining == 0 || nextRemaining >= minGroupSize)
            {
                // Immutable append using C# collection expression
                IReadOnlyList<int> nextPath = currentPath.Append(size).ToImmutableList(); // A unit test fails, when swapping to collection expression.
                
                var result = RecursivelyFigureOutGroupSizes(nextRemaining, allowedPriorities, nextPath);
                if (result != null)
                {
                    return result; // Solution found!
                }
            }
        }

        return null; // Dead end path
    }
}