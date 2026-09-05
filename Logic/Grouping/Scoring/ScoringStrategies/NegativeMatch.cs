using Logic.Models;

namespace Logic.Grouping.Scoring.ScoringStrategies;

public class NegativeMatch(double points) : IScorer
{
    public string Name { get; } = "NegativeMatch";

    public double Evaluate(GroupComposition composition) =>
        composition.Groups.Sum(EvaluateGroup);

    public double EvaluateGroup(Group group) =>
        CountNegativeMatches(group.Members) * -points;

    private static int CountNegativeMatches(IReadOnlyList<Student> members) =>
        members.SelectMany(
                a => members.Where(b => b != a), (a, b) => (a, b))
            .Count(pair => HasNegativeWish(pair.a, pair.b));

    private static bool HasNegativeWish(Student from, Student to) =>
        from.NegativeWishes.Any(wish => wish.Value == to.Number);
}
