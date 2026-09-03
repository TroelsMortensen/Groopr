using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaUI.Data.ScoringConfiguration;
using Logic.Grouping.Scoring;
using Logic.Grouping.Scoring.ScoringStrategies;

namespace AvaloniaUI.Data.ScoringConfiguration;

public static class ScorerConfigurationMapper
{
    public static GroupCompositionScorer ToScorer(IEnumerable<ScorerConfiguration> configurations)
    {
        var scorers = configurations.Select(ToScoringStrategy).ToList();
        return new GroupCompositionScorer(scorers);
    }

    public static IScorer ToScoringStrategy(ScorerConfiguration configuration) =>
        configuration switch
        {
            MutualMatchScoringConfiguration mutual => new MutualMatch(mutual.Weight),
            PartialMatchScoringConfiguration partial => new PartialMatch(partial.Weight),
            NegativeMatchScoringConfiguration negative => new NegativeMatch(negative.Weight),
            _ => throw new ArgumentOutOfRangeException(nameof(configuration), configuration, "Unknown scorer configuration.")
        };
}
