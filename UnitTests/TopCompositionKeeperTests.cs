using Logic.Grouping;
using Logic.Models;

namespace UnitTests;

public class TopCompositionKeeperTests
{
    private static int _nextPartitionId;

    #region Capacity and ordering

    [Fact]
    public void TryAdd_FewerThanCapacity_KeepsAllSortedDescending()
    {
        var keeper = new TopCompositionKeeper(5);

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(DistinctComposition(3)));
        Assert.Equal(TryAddResult.Added, keeper.TryAdd(DistinctComposition(7)));
        Assert.Equal(TryAddResult.Added, keeper.TryAdd(DistinctComposition(5)));

        Assert.Equal([7, 5, 3], keeper.Compositions.Select(c => c.TotalScore));
    }

    [Fact]
    public void TryAdd_AtCapacity_ReplacesLowestWhenBetter()
    {
        var keeper = new TopCompositionKeeper(3);
        keeper.TryAdd(DistinctComposition(10));
        keeper.TryAdd(DistinctComposition(8));
        keeper.TryAdd(DistinctComposition(6));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(DistinctComposition(7)));

        Assert.Equal([10, 8, 7], keeper.Compositions.Select(c => c.TotalScore));
    }

    [Fact]
    public void TryAdd_AtCapacity_RejectsWhenNotBetterThanLowest()
    {
        var keeper = new TopCompositionKeeper(3);
        keeper.TryAdd(DistinctComposition(10));
        keeper.TryAdd(DistinctComposition(8));
        keeper.TryAdd(DistinctComposition(6));

        Assert.Equal(TryAddResult.RejectedScoreTooLow, keeper.TryAdd(DistinctComposition(6)));
        Assert.Equal(TryAddResult.RejectedScoreTooLow, keeper.TryAdd(DistinctComposition(5)));

        Assert.Equal([10, 8, 6], keeper.Compositions.Select(c => c.TotalScore));
    }

    [Fact]
    public void TryAdd_EqualScore_DoesNotReplaceExisting()
    {
        var keeper = new TopCompositionKeeper(2);
        var first = DistinctComposition(5);
        var second = DistinctComposition(5);

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(first));
        Assert.Equal(TryAddResult.Added, keeper.TryAdd(second));
        Assert.Equal(TryAddResult.RejectedScoreTooLow, keeper.TryAdd(DistinctComposition(5)));

        Assert.Equal(2, keeper.Compositions.Count);
        Assert.Same(first, keeper.Compositions[0]);
        Assert.Same(second, keeper.Compositions[1]);
    }

    #endregion

    #region Duplicate rejection

    [Fact]
    public void TryAdd_ExactDuplicate_ReturnsRejectedAsDuplicate()
    {
        var keeper = new TopCompositionKeeper();
        var composition = Composition(
            10,
            Group("100001", "100002"),
            Group("100003", "100004"));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(composition));
        Assert.Equal(TryAddResult.RejectedAsDuplicate, keeper.TryAdd(composition));
    }

    [Fact]
    public void TryAdd_ReorderedGroupsOnly_ReturnsRejectedAsDuplicate()
    {
        var keeper = new TopCompositionKeeper();
        var original = Composition(
            10,
            Group("100001", "100002"),
            Group("100003", "100004"));
        var reorderedGroups = Composition(
            10,
            Group("100003", "100004"),
            Group("100001", "100002"));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(original));
        Assert.Equal(TryAddResult.RejectedAsDuplicate, keeper.TryAdd(reorderedGroups));
    }

    [Fact]
    public void TryAdd_ReorderedStudentsWithinGroups_ReturnsRejectedAsDuplicate()
    {
        var keeper = new TopCompositionKeeper();
        var original = Composition(
            10,
            Group("100001", "100002"),
            Group("100003", "100004"));
        var reorderedStudents = Composition(
            10,
            Group("100002", "100001"),
            Group("100004", "100003"));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(original));
        Assert.Equal(TryAddResult.RejectedAsDuplicate, keeper.TryAdd(reorderedStudents));
    }

    [Fact]
    public void TryAdd_GroupsAndStudentsBothReordered_ReturnsRejectedAsDuplicate()
    {
        var keeper = new TopCompositionKeeper();
        var original = Composition(
            10,
            Group("100001", "100002"),
            Group("100003", "100004"));
        var fullyReordered = Composition(
            10,
            Group("100004", "100003"),
            Group("100002", "100001"));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(original));
        Assert.Equal(TryAddResult.RejectedAsDuplicate, keeper.TryAdd(fullyReordered));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(100.0)]
    public void TryAdd_SamePartitionDifferentScore_ReturnsRejectedAsDuplicate(double duplicateScore)
    {
        var keeper = new TopCompositionKeeper();
        var original = Composition(
            10,
            Group("100001", "100002"),
            Group("100003", "100004"));
        var samePartitionDifferentScore = Composition(
            duplicateScore,
            Group("100002", "100001"),
            Group("100004", "100003"));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(original));
        Assert.Equal(TryAddResult.RejectedAsDuplicate, keeper.TryAdd(samePartitionDifferentScore));

        Assert.Single(keeper.Compositions);
        Assert.Same(original, keeper.Compositions[0]);
        Assert.Equal(10, keeper.Compositions[0].TotalScore);
    }

    [Fact]
    public void TryAdd_SameNumbersDifferentStudentData_ReturnsRejectedAsDuplicate()
    {
        var keeper = new TopCompositionKeeper();
        var original = Composition(
            10,
            Group(
                Student("100001", name: "Ada", wishes: ["100002"]),
                Student("100002", name: "Bob")));
        var differentData = Composition(
            10,
            Group(
                Student("100001", name: "Alice", wishes: ["100003"]),
                Student("100002", name: "Robert", wishes: ["100001"])));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(original));
        Assert.Equal(TryAddResult.RejectedAsDuplicate, keeper.TryAdd(differentData));
    }

    [Fact]
    public void TryAdd_DuplicateWhenUnderCapacity_DoesNotChangeList()
    {
        var keeper = new TopCompositionKeeper(5);
        var first = Composition(10, Group("100001", "100002"));
        var second = DistinctComposition(8);

        keeper.TryAdd(first);
        keeper.TryAdd(second);

        var result = keeper.TryAdd(Composition(99, Group("100002", "100001")));

        Assert.Equal(TryAddResult.RejectedAsDuplicate, result);
        Assert.Equal(2, keeper.Compositions.Count);
        Assert.Same(first, keeper.Compositions[0]);
        Assert.Same(second, keeper.Compositions[1]);
    }

    [Fact]
    public void TryAdd_DuplicateWhenAtCapacity_DoesNotReplaceLowest()
    {
        var keeper = new TopCompositionKeeper(2);
        var high = Composition(10, Group("100001", "100002"));
        var low = Composition(5, Group("100003", "100004"));

        keeper.TryAdd(high);
        keeper.TryAdd(low);

        var duplicateOfHighWithBetterScore = Composition(
            100,
            Group("100002", "100001"));

        Assert.Equal(TryAddResult.RejectedAsDuplicate, keeper.TryAdd(duplicateOfHighWithBetterScore)); 
        Assert.Equal([10, 5], keeper.Compositions.Select(c => c.TotalScore));
        Assert.Same(high, keeper.Compositions[0]);
        Assert.Same(low, keeper.Compositions[1]);
    }

    [Fact]
    public void TryAdd_Duplicate_DoesNotUpdateRevisionOrRecentInsertedAt()
    {
        var keeper = new TopCompositionKeeper();
        var original = Composition(10, Group("100001", "100002"));

        keeper.TryAdd(original);
        var revision = keeper.Revision;
        var recentInsertedAt = keeper.RecentInsertedAt.ToArray();

        Assert.Equal(
            TryAddResult.RejectedAsDuplicate,
            keeper.TryAdd(Composition(50, Group("100002", "100001"))));

        Assert.Equal(revision, keeper.Revision);
        Assert.Equal(recentInsertedAt, keeper.RecentInsertedAt);
    }

    [Fact]
    public void TryAdd_MultipleDuplicateAttempts_AllReturnRejectedAsDuplicate()
    {
        var keeper = new TopCompositionKeeper();
        var original = Composition(
            10,
            Group("100001", "100002"),
            Group("100003", "100004"));

        keeper.TryAdd(original);

        Assert.Equal(
            TryAddResult.RejectedAsDuplicate,
            keeper.TryAdd(Composition(10, Group("100001", "100002"), Group("100003", "100004"))));
        Assert.Equal(
            TryAddResult.RejectedAsDuplicate,
            keeper.TryAdd(Composition(20, Group("100003", "100004"), Group("100001", "100002"))));
        Assert.Equal(
            TryAddResult.RejectedAsDuplicate,
            keeper.TryAdd(Composition(1, Group("100004", "100003"), Group("100002", "100001"))));

        Assert.Single(keeper.Compositions);
        Assert.Equal(1, keeper.Revision);
    }

    [Fact]
    public void TryAdd_TwoEmptyCompositions_AreDuplicates()
    {
        var keeper = new TopCompositionKeeper();
        var first = new GroupComposition([new Group([])], 10);
        var second = new GroupComposition([new Group([])], 20);

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(first));
        Assert.Equal(TryAddResult.RejectedAsDuplicate, keeper.TryAdd(second));
    }

    [Fact]
    public void TryAdd_EmptyVersusNonEmpty_AreNotDuplicates()
    {
        var keeper = new TopCompositionKeeper();
        var empty = new GroupComposition([new Group([])], 10);
        var nonEmpty = Composition(10, Group("100001", "100002"));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(empty));
        Assert.Equal(TryAddResult.Added, keeper.TryAdd(nonEmpty));
        Assert.Equal(2, keeper.Compositions.Count);
    }

    [Fact]
    public void TryAdd_DifferentPartition_UnderCapacity_ReturnsAdded()
    {
        var keeper = new TopCompositionKeeper();

        Assert.Equal(
            TryAddResult.Added,
            keeper.TryAdd(Composition(10, Group("100001", "100002"))));
        Assert.Equal(
            TryAddResult.Added,
            keeper.TryAdd(Composition(9, Group("100001", "100003"))));
    }

    [Fact]
    public void TryAdd_DifferentPartition_ThatBeatsLowest_ReturnsAddedAndReplaces()
    {
        var keeper = new TopCompositionKeeper(2);
        keeper.TryAdd(Composition(10, Group("100001", "100002")));
        keeper.TryAdd(Composition(5, Group("100003", "100004")));

        Assert.Equal(
            TryAddResult.Added,
            keeper.TryAdd(Composition(7, Group("100005", "100006"))));

        Assert.Equal([10, 7], keeper.Compositions.Select(c => c.TotalScore));
    }

    [Fact]
    public void TryAdd_DifferentPartition_ThatDoesNotBeatLowest_ReturnsRejectedScoreTooLow()
    {
        var keeper = new TopCompositionKeeper(2);
        keeper.TryAdd(Composition(10, Group("100001", "100002")));
        keeper.TryAdd(Composition(5, Group("100003", "100004")));

        Assert.Equal(
            TryAddResult.RejectedScoreTooLow,
            keeper.TryAdd(Composition(4, Group("100005", "100006"))));

        Assert.Equal([10, 5], keeper.Compositions.Select(c => c.TotalScore));
    }

    [Fact]
    public void TryAdd_DifferentPartitioningOfSameStudents_IsNotDuplicate()
    {
        var keeper = new TopCompositionKeeper();
        var partitionA = Composition(
            10,
            Group("100001", "100002"),
            Group("100003", "100004"));
        var partitionB = Composition(
            10,
            Group("100001", "100003"),
            Group("100002", "100004"));

        Assert.Equal(TryAddResult.Added, keeper.TryAdd(partitionA));
        Assert.Equal(TryAddResult.Added, keeper.TryAdd(partitionB));
        Assert.Equal(2, keeper.Compositions.Count);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllCompositions()
    {
        var keeper = new TopCompositionKeeper();
        keeper.TryAdd(DistinctComposition(1));
        keeper.TryAdd(DistinctComposition(2));

        keeper.Clear();

        Assert.Empty(keeper.Compositions);
    }

    [Fact]
    public void Clear_AllowsPreviouslyRejectedDuplicateToBeAddedAgain()
    {
        var keeper = new TopCompositionKeeper();
        var composition = Composition(10, Group("100001", "100002"));

        keeper.TryAdd(composition);
        Assert.Equal(
            TryAddResult.RejectedAsDuplicate,
            keeper.TryAdd(Composition(20, Group("100002", "100001"))));

        keeper.Clear();

        Assert.Equal(
            TryAddResult.Added,
            keeper.TryAdd(Composition(20, Group("100002", "100001"))));
    }

    #endregion

    #region RecentInsertedAt

    [Fact]
    public void TryAdd_RecordsRecentInsertedAt()
    {
        var keeper = new TopCompositionKeeper();
        var before = DateTime.UtcNow;

        keeper.TryAdd(DistinctComposition(1));

        Assert.Single(keeper.RecentInsertedAt);
        Assert.InRange(keeper.RecentInsertedAt[0], before, DateTime.UtcNow);
    }

    [Fact]
    public void TryAdd_PrependsRecentInsertedAt_AndCapsAtCapacity()
    {
        var keeper = new TopCompositionKeeper(3);

        keeper.TryAdd(DistinctComposition(1));
        Assert.Single(keeper.RecentInsertedAt);

        var afterFirst = keeper.RecentInsertedAt[0];
        keeper.TryAdd(DistinctComposition(2));
        Assert.Equal(2, keeper.RecentInsertedAt.Count);
        Assert.Equal(afterFirst, keeper.RecentInsertedAt[1]);

        var afterSecond = keeper.RecentInsertedAt.ToArray();
        keeper.TryAdd(DistinctComposition(3));
        Assert.Equal(3, keeper.RecentInsertedAt.Count);
        Assert.Equal(afterSecond[0], keeper.RecentInsertedAt[1]);
        Assert.Equal(afterSecond[1], keeper.RecentInsertedAt[2]);

        var afterThird = keeper.RecentInsertedAt.ToArray();
        keeper.TryAdd(DistinctComposition(4));
        Assert.Equal(3, keeper.RecentInsertedAt.Count);
        Assert.Equal(afterThird[0], keeper.RecentInsertedAt[1]);
        Assert.Equal(afterThird[1], keeper.RecentInsertedAt[2]);
    }

    [Fact]
    public void TryAdd_RejectedScoreTooLow_DoesNotUpdateRecentInsertedAt()
    {
        var keeper = new TopCompositionKeeper(1);
        keeper.TryAdd(DistinctComposition(10));
        var recentInsertedAt = keeper.RecentInsertedAt.ToArray();

        Assert.Equal(TryAddResult.RejectedScoreTooLow, keeper.TryAdd(DistinctComposition(5)));

        Assert.Equal(recentInsertedAt, keeper.RecentInsertedAt);
    }

    [Fact]
    public void Clear_ClearsRecentInsertedAt()
    {
        var keeper = new TopCompositionKeeper();
        keeper.TryAdd(DistinctComposition(1));

        keeper.Clear();

        Assert.Empty(keeper.RecentInsertedAt);
    }

    #endregion

    #region Revision

    [Fact]
    public void TryAdd_IncrementsRevision()
    {
        var keeper = new TopCompositionKeeper();

        keeper.TryAdd(DistinctComposition(1));
        keeper.TryAdd(DistinctComposition(2));

        Assert.Equal(2, keeper.Revision);
    }

    [Fact]
    public void TryAdd_RejectedScoreTooLow_DoesNotIncrementRevision()
    {
        var keeper = new TopCompositionKeeper(1);
        keeper.TryAdd(DistinctComposition(10));

        Assert.Equal(TryAddResult.RejectedScoreTooLow, keeper.TryAdd(DistinctComposition(5)));

        Assert.Equal(1, keeper.Revision);
    }

    [Fact]
    public void Clear_ResetsRevision()
    {
        var keeper = new TopCompositionKeeper();
        keeper.TryAdd(DistinctComposition(1));

        keeper.Clear();

        Assert.Equal(0, keeper.Revision);
    }

    #endregion

    private static GroupComposition DistinctComposition(double score)
    {
        var id = Interlocked.Increment(ref _nextPartitionId);
        var first = (200000 + id * 2).ToString("D6");
        var second = (200000 + id * 2 + 1).ToString("D6");
        return Composition(score, Group(first, second));
    }

    private static GroupComposition Composition(double score, params Group[] groups) =>
        new(groups, score);

    private static Group Group(params string[] memberNumbers) =>
        new(memberNumbers.Select(n => Student(n)).ToArray());

    private static Group Group(params Student[] members) => new(members);

    private static Student Student(
        string number,
        string? name = null,
        params string[] wishes) =>
        Logic.Models.Student.Create(
            number,
            wishes.Select(StudentNumber.Create).ToArray(),
            name);
}
