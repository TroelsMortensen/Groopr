using Logic.Models;

namespace Logic.Grouping.Scoring.ScoringStrategies;

public interface IScorer
{
    string Name  { get; }
    double Evaluate(GroupComposition composition);
}