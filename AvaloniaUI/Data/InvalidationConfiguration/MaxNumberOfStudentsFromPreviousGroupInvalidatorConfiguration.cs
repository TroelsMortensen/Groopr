using System;

namespace AvaloniaUI.Data.InvalidationConfiguration;

public sealed record MaxNumberOfStudentsFromPreviousGroupInvalidatorConfiguration(int MaxCount)
    : InvalidatorConfiguration
{
    public static MaxNumberOfStudentsFromPreviousGroupInvalidatorConfiguration Create(int maxCount)
    {
        if (maxCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), "Maximum count must be 1 or more.");
        }

        return new MaxNumberOfStudentsFromPreviousGroupInvalidatorConfiguration(maxCount);
    }
}
