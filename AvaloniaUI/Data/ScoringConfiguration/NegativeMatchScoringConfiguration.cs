using System;

namespace AvaloniaUI.Data.ScoringConfiguration;

public sealed record NegativeMatchScoringConfiguration(double Weight) : ScorerConfiguration
{
    public static NegativeMatchScoringConfiguration Create(double weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than 0.");
        }

        return new NegativeMatchScoringConfiguration(weight);
    }
}
