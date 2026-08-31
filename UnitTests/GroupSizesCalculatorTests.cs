using System.Diagnostics;
using Logic.GroupSizing;
using Logic.Models;

namespace UnitTests;

public class GroupSizesCalculatorTests
{
    #region Straightforward divisions

    [Theory]
    [InlineData(100, new[] { 4, 5, 3 })]
    [InlineData(20, new[] { 5, 4, 3 })]
    [InlineData(99, new[] { 3, 4, 5 })]
    [InlineData(24, new[] { 12, 10, 11 })]
    [InlineData(88, new[] { 11, 12, 10 })]
    public void StudentCountDivisibleByFirstChoice_UsesOnlyFirstChoiceGroups(int numberOfStudents, int[] priorities)
    {
        var groupSizes = GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities));

        Assert.Equal(numberOfStudents / priorities[0], groupSizes.Sizes.Count);
        Assert.All(groupSizes.Sizes, size => Assert.Equal(priorities[0], size));
    }

    [Theory]
    [InlineData(1, new[] { 4, 5, 3 })]
    [InlineData(2, new[] { 4, 5, 3 })]
    [InlineData(9, new[] { 11, 12, 10 })]
    [InlineData(6, new[] { 7 })]
    public void StudentCountBelowSmallestGroupSize_Throws(int numberOfStudents, int[] priorities)
    {
        Assert.ThrowsAny<Exception>(()=> GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    [Theory]
    [InlineData(3, new[] { 4, 5, 3 })]
    [InlineData(3, new[] { 3, 4, 5 })]
    [InlineData(10, new[] { 11, 12, 10 })]
    [InlineData(7, new[] { 7 })]
    public void StudentCountEqualToSmallestGroupSize_FillsExactlyOneGroup(int numberOfStudents, int[] priorities)
    {
        Assert.Equal([numberOfStudents], GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    [Theory]
    [InlineData(4, new[] { 4, 5, 3 }, new[] { 4 })]
    [InlineData(5, new[] { 4, 5, 3 }, new[] { 5 })]
    [InlineData(4, new[] { 3, 4, 5 }, new[] { 4 })]
    [InlineData(11, new[] { 11, 12, 10 }, new[] { 11 })]
    [InlineData(12, new[] { 11, 12, 10 }, new[] { 12 })]
    public void ClassThatFillsExactlyOneAcceptableGroup_ProducesASingleGroup(int numberOfStudents, int[] priorities, int[] expected)
    {
        Assert.Equal(expected, GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    [Theory]
    [InlineData(12, new[] { 4 }, new[] { 4, 4, 4 })]
    [InlineData(21, new[] { 7 }, new[] { 7, 7, 7 })]
    public void SingleAcceptableSize_RepeatsThatSize(int numberOfStudents, int[] priorities, int[] expected)
    {
        Assert.Equal(expected, GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    #endregion

    #region Honouring the ranked priorities

    [Theory]
    [InlineData(22, new[] { 5, 4, 3 }, new[] { 5, 5, 4, 4, 4 })]
    [InlineData(23, new[] { 4, 5, 3 }, new[] { 4, 4, 5, 5, 5 })]
    [InlineData(45, new[] { 4, 5, 3 }, new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5 })]
    [InlineData(13, new[] { 4, 5, 3 }, new[] { 4, 4, 5 })]
    [InlineData(100, new[] { 11, 12, 10 }, new[] { 11, 11, 11, 11, 11, 11, 11, 11, 12 })]
    public void LeftoverStudents_AreAbsorbedByTheSecondChoiceSize(int numberOfStudents, int[] priorities, int[] expected)
    {
        Assert.Equal(expected, GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    [Theory]
    [InlineData(6, new[] { 4, 5, 3 }, new[] { 3, 3 })]
    [InlineData(7, new[] { 4, 5, 3 }, new[] { 4, 3 })]
    [InlineData(11, new[] { 4, 5, 3 }, new[] { 4, 4, 3 })]
    [InlineData(20, new[] { 11, 12, 10 }, new[] { 10, 10 })]
    [InlineData(21, new[] { 11, 12, 10 }, new[] { 11, 10 })]
    public void LastChoiceSize_IsUsedOnlyWhenTheBetterSizesCannotFillTheClass(int numberOfStudents, int[] priorities, int[] expected)
    {
        Assert.Equal(expected, GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    [Fact]
    public void AvoidingALowerRankedSize_OutweighsMakingMoreFirstChoiceGroups()
    {
        // 23 students could be five groups of 4 plus one group of 3, which yields more
        // first-choice groups, but the calculator refuses to touch the third choice while
        // the first two groupSizeDistributions can still fill the class on their own.
        var groupSizeDistributions = GroupSizesCalculator.DetermineGroupSizes(23, GroupSizePriorities.Create([4, 5, 3]));

        Assert.Equal([4, 4, 5, 5, 5], groupSizeDistributions.Sizes);
        Assert.DoesNotContain(3, groupSizeDistributions.Sizes);
    }

    [Fact]
    public void SizePriorityOrder_ChangesTheResultForTheSameStudentCount()
    {
        Assert.Equal([4, 4, 5, 5, 5], GroupSizesCalculator.DetermineGroupSizes(23, GroupSizePriorities.Create([4, 5, 3])).Sizes);
        Assert.Equal([5, 5, 5, 4, 4], GroupSizesCalculator.DetermineGroupSizes(23, GroupSizePriorities.Create([5, 4, 3])).Sizes);
        Assert.Equal([3, 3, 3, 3, 3, 4, 4], GroupSizesCalculator.DetermineGroupSizes(23, GroupSizePriorities.Create([3, 4, 5])).Sizes);
    }

    [Theory]
    [InlineData(8, new[] { 12, 4 }, new[] { 4, 4 })]
    [InlineData(10, new[] { 12, 5 }, new[] { 5, 5 })]
    public void ClassTooSmallForTheFirstChoice_UsesALowerRankedSize(int numberOfStudents, int[] priorities, int[] expected)
    {
        Assert.Equal(expected, GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    [Fact]
    public void ExtraLowerRankedSizes_AreIgnoredWhenTheyAreNotNeeded()
    {
        int[] withSpareSizes = [4, 5, 3, 6, 2];

        Assert.Equal([4, 4, 5, 5, 5], GroupSizesCalculator.DetermineGroupSizes(23, GroupSizePriorities.Create(withSpareSizes)).Sizes);
    }

    #endregion

    #region Impossible class groupSizeDistributions

    [Theory]
    [InlineData(13, new[] { 11, 12, 10 })]
    [InlineData(19, new[] { 11, 12, 10 })]
    [InlineData(25, new[] { 11, 12, 10 })]
    [InlineData(29, new[] { 11, 12, 10 })]
    [InlineData(9, new[] { 4 })]
    [InlineData(99, new[] { 6, 8 })]
    public void StudentCountThatNoCombinationCanCover_Throws(int numberOfStudents, int[] priorities)
    {
        Assert.ThrowsAny<Exception>(
            () => GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);

        // Assert.Contains(numberOfStudents.ToString(), exception.Message);
        // Assert.Contains(string.Join(", ", GroupSizePriorities.Create(priorities)), exception.Message);
    }

    [Theory]
    [InlineData(30, new[] { 11, 12, 10 }, new[] { 10, 10, 10 })]
    [InlineData(31, new[] { 11, 12, 10 }, new[] { 11, 10, 10 })]
    [InlineData(32, new[] { 11, 12, 10 }, new[] { 11, 11, 10 })]
    [InlineData(33, new[] { 11, 12, 10 }, new[] { 11, 11, 11 })]
    public void StudentCountJustAboveAnImpossibleRange_IsSolvedAgain(int numberOfStudents, int[] priorities, int[] expected)
    {
        Assert.Equal(expected, GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    #endregion

    #region Degenerate input

    [Fact]
    public void NoStudents_IsRejected()
    {
        Assert.ThrowsAny<Exception>(() => GroupSizesCalculator.DetermineGroupSizes(0, GroupSizePriorities.Create([4, 5, 3])));
    }

    [Fact]
    public void NegativeStudentCount_IsRejected()
    {
        Assert.ThrowsAny<Exception>(() => GroupSizesCalculator.DetermineGroupSizes(-5, GroupSizePriorities.Create([4, 5, 3])));
    }

    [Fact]
    public void EmptyPriorityList_IsRejected()
    {
        Assert.ThrowsAny<Exception>(() => GroupSizesCalculator.DetermineGroupSizes(20, GroupSizePriorities.Create([])));
    }

    [Fact]
    public void MissingPriorityList_IsRejected()
    {
        Assert.ThrowsAny<Exception>(() => GroupSizesCalculator.DetermineGroupSizes(20, null!));
    }

    [Fact]
    public void GroupSizeOfZero_IsRejected()
    {
        Assert.ThrowsAny<Exception>(() => GroupSizesCalculator.DetermineGroupSizes(20, GroupSizePriorities.Create([0, 4])));
    }


    [Fact]
    public void PriorityList_IsNotModified()
    {
        int[] priorities = [4, 5, 3];

        GroupSizesCalculator.DetermineGroupSizes(23, GroupSizePriorities.Create(priorities));

        Assert.Equal(new[] { 4, 5, 3 }, GroupSizePriorities.Create(priorities).Sizes);
    }


    #endregion

    #region Full sweep of 1 to 100 students

    [Theory]
    [MemberData(nameof(StudentCountsWithPrioritySets))]
    public void AnyClassThatCanBeDivided_ProducesAValidBlueprint(int numberOfStudents, int[] priorities)
    {
        if (!CanBeDivided(numberOfStudents, GroupSizePriorities.Create(priorities).Sizes))
        {
            Assert.ThrowsAny<Exception>(() => GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
            return;
        }

        var groupSizeDistributions = GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities));

        Assert.NotEmpty(groupSizeDistributions.Sizes);
        Assert.Equal(numberOfStudents, groupSizeDistributions.Sizes.Sum());
        Assert.All(groupSizeDistributions.Sizes, size => Assert.Contains(size, GroupSizePriorities.Create(priorities).Sizes));

        List<int> ranks = groupSizeDistributions.Sizes.Select(size => Array.IndexOf(priorities, size)).ToList();
        Assert.Equal(ranks.OrderBy(rank => rank), ranks);
    }

    [Theory]
    [MemberData(nameof(StudentCountsWithPrioritySets))]
    public void AnyClass_MatchesTheIndependentlyCalculatedBlueprint(int numberOfStudents, int[] priorities)
    {
        List<int>? expected = ExpectedGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities).Sizes);

        if (expected is null)
        {
            Assert.ThrowsAny<Exception>(() => GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
            return;
        }

        Assert.Equal(expected, GroupSizesCalculator.DetermineGroupSizes(numberOfStudents, GroupSizePriorities.Create(priorities)).Sizes);
    }

    public static TheoryData<int, int[]> StudentCountsWithPrioritySets()
    {
        int[][] prioritySets =
        [
            [4, 5, 3],
            [5, 4, 3],
            [3, 4, 5],
            [11, 12, 10],
            [4, 3],
            [6, 8],
            [7],
            [2, 3],
            [5, 4, 3, 6],
        ];

        TheoryData<int, int[]> data = new();
        foreach (int[] priorities in prioritySets)
        {
            for (int numberOfStudents = 1; numberOfStudents <= 100; numberOfStudents++)
            {
                data.Add(numberOfStudents, GroupSizePriorities.Create(priorities).Sizes);
            }
        }

        return data;
    }

    #endregion

    #region Reference implementation used as an oracle

    /// <summary>
    /// A class has to fill at least the smallest acceptable group, and the count has to be
    /// reachable by adding up acceptable groupSizeDistributions.
    /// </summary>
    private static bool CanBeDivided(int numberOfStudents, int[] priorities)
        => numberOfStudents >= priorities.Min() && CanBeSummedFrom(numberOfStudents, GroupSizePriorities.Create(priorities).Sizes);

    /// <summary>
    /// Derives the expected blueprint from the specification rather than from the algorithm:
    /// use the shortest possible prefix of the ranked groupSizeDistributions, and within that prefix make as many
    /// groups of the best size as possible, then as many of the next best, and so on.
    /// Returns null when the class cannot be divided at all.
    /// </summary>
    private static List<int>? ExpectedGroupSizes(int numberOfStudents, int[] priorities)
    {
        if (!CanBeDivided(numberOfStudents, GroupSizePriorities.Create(priorities).Sizes))
        {
            return null;
        }

        for (int prefixLength = 1; prefixLength <= priorities.Length; prefixLength++)
        {
            int[] acceptableSizes = priorities[..prefixLength];
            if (!CanBeSummedFrom(numberOfStudents, acceptableSizes))
            {
                continue;
            }

            List<int> groupSizeDistributions = [];
            int remaining = numberOfStudents;

            for (int rank = 0; rank < acceptableSizes.Length; rank++)
            {
                int size = acceptableSizes[rank];
                int[] lowerRankedSizes = acceptableSizes[(rank + 1)..];

                int count = remaining / size;
                while (!CanBeSummedFrom(remaining - (count * size), lowerRankedSizes))
                {
                    count--;
                }

                groupSizeDistributions.AddRange(Enumerable.Repeat(size, count));
                remaining -= count * size;
            }

            return groupSizeDistributions;
        }

        return null;
    }

    private static bool CanBeSummedFrom(int total, int[] groupSizeDistributions)
    {
        if (total == 0)
        {
            return true;
        }

        if (total < 0)
        {
            return false;
        }

        bool[] reachable = new bool[total + 1];
        reachable[0] = true;

        for (int amount = 1; amount <= total; amount++)
        {
            reachable[amount] = groupSizeDistributions.Any(size => size > 0 && size <= amount && reachable[amount - size]);
        }

        return reachable[total];
    }

    #endregion
}
