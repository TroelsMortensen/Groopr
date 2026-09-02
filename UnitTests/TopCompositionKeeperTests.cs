using Logic.Grouping;
using Logic.Models;

namespace UnitTests;

public class TopCompositionKeeperTests
{
    #region Capacity and ordering

    [Fact]
    public void TryAdd_FewerThanCapacity_KeepsAllSortedDescending()
    {
        var keeper = new TopCompositionKeeper(5);

        Assert.True(keeper.TryAdd(Composition(3)));
        Assert.True(keeper.TryAdd(Composition(7)));
        Assert.True(keeper.TryAdd(Composition(5)));

        Assert.Equal([7, 5, 3], keeper.Compositions.Select(c => c.TotalScore));
    }

    [Fact]
    public void TryAdd_AtCapacity_ReplacesLowestWhenBetter()
    {
        var keeper = new TopCompositionKeeper(3);
        keeper.TryAdd(Composition(10));
        keeper.TryAdd(Composition(8));
        keeper.TryAdd(Composition(6));

        Assert.True(keeper.TryAdd(Composition(7)));

        Assert.Equal([10, 8, 7], keeper.Compositions.Select(c => c.TotalScore));
    }

    [Fact]
    public void TryAdd_AtCapacity_RejectsWhenNotBetterThanLowest()
    {
        var keeper = new TopCompositionKeeper(3);
        keeper.TryAdd(Composition(10));
        keeper.TryAdd(Composition(8));
        keeper.TryAdd(Composition(6));

        Assert.False(keeper.TryAdd(Composition(6)));
        Assert.False(keeper.TryAdd(Composition(5)));

        Assert.Equal([10, 8, 6], keeper.Compositions.Select(c => c.TotalScore));
    }

    [Fact]
    public void TryAdd_EqualScore_DoesNotReplaceExisting()
    {
        var keeper = new TopCompositionKeeper(2);
        var first = Composition(5);
        var second = Composition(5);

        Assert.True(keeper.TryAdd(first));
        Assert.True(keeper.TryAdd(second));
        Assert.False(keeper.TryAdd(Composition(5)));

        Assert.Equal(2, keeper.Compositions.Count);
        Assert.Same(first, keeper.Compositions[0]);
        Assert.Same(second, keeper.Compositions[1]);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllCompositions()
    {
        var keeper = new TopCompositionKeeper();
        keeper.TryAdd(Composition(1));
        keeper.TryAdd(Composition(2));

        keeper.Clear();

        Assert.Empty(keeper.Compositions);
    }

    #endregion

    #region LastInsertedAt

    [Fact]
    public void TryAdd_SetsLastInsertedAt()
    {
        var keeper = new TopCompositionKeeper();
        var before = DateTime.UtcNow;

        keeper.TryAdd(Composition(1));

        Assert.NotNull(keeper.LastInsertedAt);
        Assert.InRange(keeper.LastInsertedAt!.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public void TryAdd_Rejected_DoesNotUpdateLastInsertedAt()
    {
        var keeper = new TopCompositionKeeper(1);
        keeper.TryAdd(Composition(10));
        var firstInsertedAt = keeper.LastInsertedAt;

        Assert.False(keeper.TryAdd(Composition(5)));

        Assert.Equal(firstInsertedAt, keeper.LastInsertedAt);
    }

    [Fact]
    public void Clear_ClearsLastInsertedAt()
    {
        var keeper = new TopCompositionKeeper();
        keeper.TryAdd(Composition(1));

        keeper.Clear();

        Assert.Null(keeper.LastInsertedAt);
    }

    #endregion

    #region Revision

    [Fact]
    public void TryAdd_IncrementsRevision()
    {
        var keeper = new TopCompositionKeeper();

        keeper.TryAdd(Composition(1));
        keeper.TryAdd(Composition(2));

        Assert.Equal(2, keeper.Revision);
    }

    [Fact]
    public void TryAdd_Rejected_DoesNotIncrementRevision()
    {
        var keeper = new TopCompositionKeeper(1);
        keeper.TryAdd(Composition(10));

        Assert.False(keeper.TryAdd(Composition(5)));

        Assert.Equal(1, keeper.Revision);
    }

    [Fact]
    public void Clear_ResetsRevision()
    {
        var keeper = new TopCompositionKeeper();
        keeper.TryAdd(Composition(1));

        keeper.Clear();

        Assert.Equal(0, keeper.Revision);
    }

    #endregion

    private static GroupComposition Composition(double score) =>
        new([new Group([])], score);
}
