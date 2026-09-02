using Logic.Models;

namespace Logic.Grouping;

public class TopCompositionKeeper(int capacity = 5)
{
    private readonly List<GroupComposition> _compositions = [];
    private readonly int _capacity = capacity;

    public IReadOnlyList<GroupComposition> Compositions => _compositions;

    public DateTime? LastInsertedAt { get; private set; }

    public long Revision { get; private set; }

    public bool TryAdd(GroupComposition composition)
    {
        if (_compositions.Count < _capacity)
        {
            _compositions.Add(composition);
            SortDescending();
            RecordInsertion();
            return true;
        }

        var lowestScore = _compositions[^1].TotalScore;
        if (composition.TotalScore <= lowestScore)
        {
            return false;
        }

        _compositions.RemoveAt(_compositions.Count - 1);
        _compositions.Add(composition);
        SortDescending();
        RecordInsertion();
        return true;
    }

    public void Clear()
    {
        _compositions.Clear();
        LastInsertedAt = null;
        Revision = 0;
    }

    private void RecordInsertion()
    {
        LastInsertedAt = DateTime.UtcNow;
        Revision++;
    }

    private void SortDescending() =>
        _compositions.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));
}
