namespace Logic.Models;

public record GroupComposition(IReadOnlyList<Group> Groups, double TotalScore = 0.0)
{
    public virtual bool Equals(GroupComposition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        var left = Normalize(Groups);
        var right = Normalize(other.Groups);
        if (left.Count != right.Count) return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!left[i].SequenceEqual(right[i], StringComparer.Ordinal))
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var group in Normalize(Groups))
        {
            var groupHash = new HashCode();
            foreach (var number in group)
                groupHash.Add(number, StringComparer.Ordinal);
            hash.Add(groupHash.ToHashCode());
        }

        return hash.ToHashCode();
    }

    private static List<string[]> Normalize(IReadOnlyList<Group> groups) =>
        groups
            .Select(g => g.Members
                .Select(s => s.Number)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray())
            .OrderBy(g => string.Join(',', g), StringComparer.Ordinal)
            .ToList();
}
