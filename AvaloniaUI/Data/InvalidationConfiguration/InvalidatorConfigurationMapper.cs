using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Grouping.Invalidation;
using Logic.Grouping.Invalidation.InvalidationStrategies;

namespace AvaloniaUI.Data.InvalidationConfiguration;

public static class InvalidatorConfigurationMapper
{
    public static GroupCompositionInvalidator ToInvalidator(IEnumerable<InvalidatorConfiguration> configurations)
    {
        var invalidators = configurations.Select(ToInvalidationStrategy).ToList();
        return new GroupCompositionInvalidator(invalidators);
    }

    public static IInvalidator ToInvalidationStrategy(InvalidatorConfiguration configuration) =>
        configuration switch
        {
            MaxNumberOfStudentsFromPreviousGroupInvalidatorConfiguration maxPrevious =>
                new MaxNumberOfStudentsFromPreviousGroup(maxPrevious.MaxCount),
            _ => throw new ArgumentOutOfRangeException(nameof(configuration), configuration, "Unknown invalidator configuration.")
        };
}
