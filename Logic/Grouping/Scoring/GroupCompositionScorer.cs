using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.Models;

namespace Logic.Grouping.Scoring;

public class GroupCompositionScorer(List<IScorer> scorers)
{
    public double ScoreGroup(Group group) =>
        scorers.Sum(scorer => scorer.EvaluateGroup(group));

    public GroupComposition Score(GroupComposition composition)
    {
        double totalScore = scorers.Sum(scorer => scorer.Evaluate(composition));
        return composition with { TotalScore = totalScore };
    }
}