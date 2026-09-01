using System;

namespace AvaloniaUI.Data;

public sealed record MutualMatchConfiguration(double Weight) : ScorerConfigurationRecord
{
    public static MutualMatchConfiguration Create(double weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than 0.");
        }

        return new MutualMatchConfiguration(weight);
    }
}
