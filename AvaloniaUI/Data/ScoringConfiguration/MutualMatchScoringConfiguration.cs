using System;

namespace AvaloniaUI.Data.ScoringConfiguration;

public sealed record MutualMatchScoringConfiguration(double Weight) : ScorerConfiguration
{
    public static MutualMatchScoringConfiguration Create(double weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than 0.");
        }

        return new MutualMatchScoringConfiguration(weight);
    }
}
