using Logic.Models;

namespace Logic.Grouping;

public enum TryAddResult
{
    Added,
    RejectedAsDuplicate,
    RejectedScoreTooLow
}

public class TopCompositionKeeper(int capacity = 5)
{
    private readonly List<GroupComposition> _compositions = [];
    private readonly List<DateTime> _recentInsertedAt = [];
    private readonly int _capacity = capacity;

    public IReadOnlyList<GroupComposition> Compositions => _compositions;

    public IReadOnlyList<DateTime> RecentInsertedAt => _recentInsertedAt;

    public long Revision { get; private set; }

    public TryAddResult TryAdd(GroupComposition composition)
    {
        if (_compositions.Contains(composition))
        {
            return TryAddResult.RejectedAsDuplicate;
        }

        if (_compositions.Count < _capacity)
        {
            _compositions.Add(composition);
            SortDescending();
            RecordInsertion();
            return TryAddResult.Added;
        }

        var lowestScore = _compositions[^1].TotalScore;
        if (composition.TotalScore <= lowestScore)
        {
            return TryAddResult.RejectedScoreTooLow;
        }

        _compositions.RemoveAt(_compositions.Count - 1);
        _compositions.Add(composition);
        SortDescending();
        RecordInsertion();
        return TryAddResult.Added;
    }

    public void Clear()
    {
        _compositions.Clear();
        _recentInsertedAt.Clear();
        Revision = 0;
    }

    private void RecordInsertion()
    {
        _recentInsertedAt.Insert(0, DateTime.UtcNow);
        if (_recentInsertedAt.Count > _capacity)
        {
            _recentInsertedAt.RemoveAt(_recentInsertedAt.Count - 1);
        }

        Revision++;
    }

    private void SortDescending() =>
        _compositions.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));
}
