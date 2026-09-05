using Logic.Models;

namespace Logic.Grouping.Scoring.ScoringStrategies;

public class PartialMatch(double points) : IScorer
{
    public string Name { get; } = "PartialMatch";

    public double Evaluate(GroupComposition composition) =>
        composition.Groups.Sum(EvaluateGroup);

    public double EvaluateGroup(Group group) =>
        CountPartialMatches(group.Members) * points;

    private static int CountPartialMatches(IReadOnlyList<Student> members) =>
        // Check every ordered pair (A -> B and B -> A separately)
        members.SelectMany(
                a => members.Where(b => b != a), (a, b) => (a, b)
            )
            .Count(pair => IsOneWayWish(pair.a, pair.b));

    private static bool IsOneWayWish(Student a, Student b) =>
        WishesStudent(a, b) && !WishesStudent(b, a);

    private static bool WishesStudent(Student from, Student to) =>
        from.PositiveWishes.Any(wish => wish.Value == to.Number);
}
