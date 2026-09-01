using System;

namespace AvaloniaUI.Data.ScoringConfiguration;

public sealed record PartialMatchScoringConfiguration(double Weight) : ScorerConfiguration
{
    public static PartialMatchScoringConfiguration Create(double weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than 0.");
        }

        return new PartialMatchScoringConfiguration(weight);
    }
}
