using Logic.Models;

namespace Logic.Grouping.Scoring.ScoringStrategies;

public class MutualMatch(double points) : IScorer
{
    public string Name { get; } = "MutualMatch";

    public double Evaluate(GroupComposition composition) =>
        composition.Groups
            .Sum(group => CountMutualPairs(group.Members) * points
            );

    private static int CountMutualPairs(IReadOnlyList<Student> members) =>
        members.SelectMany(
                (a, i) => members.Skip(i + 1),
                (a, b) => (a, b))
            .Count(pair => AreMutualMatch(pair.a, pair.b));


    private static bool AreMutualMatch(Student a, Student b) =>
        WishesStudent(a, b) && WishesStudent(b, a);

    private static bool WishesStudent(Student from, Student to) =>
        from.PositiveWishes.Any(wish => wish.Value == to.Number);
}