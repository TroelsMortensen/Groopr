using Logic.Grouping.Scoring;
using Logic.Models;

namespace Logic.Grouping.Generation;


/// <summary>
/// Wraps an inner group composition producer and improves the groups by swapping students between groups.
/// The idea is the generate a good group, and see if it can be improved upon.
/// However, it is about 100x slower than e.g. the MutualPairFirstStrategy, and in my benchmarks it is only marginally better.
/// Not worth it, but fun.
/// Further problems are that it may produce an invalid group, which is only check before scoring.
/// Optimizations would to expose a ScoreGroup function, and only re-score the affected groups.
/// </summary>
public class HillClimbingWrapper : IGroupCompositionProducer
{
    private readonly IGroupCompositionProducer inner;
    private readonly GroupCompositionScorer scorer;
    private readonly int iterations;

    /// <summary>
    /// Optional swap proposer for tests. Arguments are mutable member lists per group.
    /// Returns (groupIndexA, memberIndexA, groupIndexB, memberIndexB), or null to skip.
    /// </summary>
    internal Func<IReadOnlyList<List<Student>>, (int GroupA, int IndexA, int GroupB, int IndexB)?> ProposeSwap
    {
        get;
        set;
    }

    public HillClimbingWrapper(
        StudentList studentList,
        GroupSizeDistribution groupSizes,
        GroupCompositionScorer scorer,
        int iterations = 75)
        : this(new MutualPairFirstStrategy(studentList, groupSizes), scorer, iterations)
    {
    }

    public HillClimbingWrapper(
        IGroupCompositionProducer inner,
        GroupCompositionScorer scorer,
        int iterations = 75)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(scorer);

        if (iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), iterations, "Iterations must be at least 1.");
        }

        this.inner = inner;
        this.scorer = scorer;
        this.iterations = iterations;
        ProposeSwap = ProposeRandomSwap;
    }

    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
    {
        foreach (GroupComposition baseline in inner.GenerateStream(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return Polish(baseline);
        }
    }

    private GroupComposition Polish(GroupComposition baseline)
    {
        List<List<Student>> working = baseline.Groups
            .Select(group => group.Members.ToList())
            .ToList();

        double bestScore = scorer.Score(ToComposition(working)).TotalScore;

        for (int i = 0; i < iterations; i++)
        {
            var proposal = ProposeSwap(working);
            if (proposal is null)
            {
                continue;
            }

            (int groupA, int indexA, int groupB, int indexB) = proposal.Value;

            Student studentA = working[groupA][indexA];
            Student studentB = working[groupB][indexB];
            working[groupA][indexA] = studentB;
            working[groupB][indexB] = studentA;

            double candidateScore = scorer.Score(ToComposition(working)).TotalScore;
            if (candidateScore > bestScore)
            {
                bestScore = candidateScore;
            }
            else
            {
                working[groupA][indexA] = studentA;
                working[groupB][indexB] = studentB;
            }
        }

        // Freshly generated compositions remain unscored for the outer pipeline.
        return ToComposition(working) with { TotalScore = 0 };
    }

    private static GroupComposition ToComposition(IReadOnlyList<List<Student>> groups) =>
        new(groups.Select(members => new Group(members.ToList())).ToList());

    private static (int GroupA, int IndexA, int GroupB, int IndexB)? ProposeRandomSwap(
        IReadOnlyList<List<Student>> groups)
    {
        if (groups.Count < 2)
        {
            return null;
        }

        int groupA = Random.Shared.Next(groups.Count);
        int groupB = Random.Shared.Next(groups.Count - 1);
        if (groupB >= groupA)
        {
            groupB++;
        }

        if (groups[groupA].Count == 0 || groups[groupB].Count == 0)
        {
            return null;
        }

        int indexA = Random.Shared.Next(groups[groupA].Count);
        int indexB = Random.Shared.Next(groups[groupB].Count);
        return (groupA, indexA, groupB, indexB);
    }
}
