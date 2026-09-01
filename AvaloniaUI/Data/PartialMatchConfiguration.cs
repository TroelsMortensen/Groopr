using System;

namespace AvaloniaUI.Data;

public sealed record PartialMatchConfiguration(double Weight) : ScorerConfigurationRecord
{
    public static PartialMatchConfiguration Create(double weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than 0.");
        }

        return new PartialMatchConfiguration(weight);
    }
}
