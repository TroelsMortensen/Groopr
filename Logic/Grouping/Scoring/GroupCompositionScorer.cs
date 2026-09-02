using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace Logic.Grouping.Scoring;

public class GroupCompositionScorer(List<IScorer> scorers)
{
    public GroupComposition Score(GroupComposition composition)
    {
        double totalScore = scorers.Sum(scorer => scorer.Evaluate(composition));
        return composition with { TotalScore = totalScore };
    }
}