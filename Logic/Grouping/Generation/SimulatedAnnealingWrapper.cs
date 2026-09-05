using Logic.Grouping.Scoring;
using Logic.Models;

namespace Logic.Grouping.Generation;

/// <summary>
/// Wraps an inner group composition producer and refines each baseline with simulated annealing:
/// random inter-group swaps accepted by the Metropolis rule while temperature cools.
/// Tracks the global best layout separately from the wandering current state.
/// Uses per-group delta scoring (only the two swapped groups are re-scored).
/// Yields polished compositions still unscored for the outer pipeline.
/// </summary>
public class SimulatedAnnealingWrapper : IGroupCompositionProducer
{
    private readonly IGroupCompositionProducer? inner;
    private readonly GroupCompositionScorer scorer;
    private readonly SimulatedAnnealingConfig config;
    private readonly int? knownStudentCount;

    /// <summary>
    /// Optional swap proposer for tests. Arguments are mutable member lists per group.
    /// Returns (groupIndexA, memberIndexA, groupIndexB, memberIndexB), or null to skip.
    /// </summary>
    internal Func<IReadOnlyList<List<Student>>, (int GroupA, int IndexA, int GroupB, int IndexB)?> ProposeSwap
    {
        get;
        set;
    }

    /// <summary>
    /// Optional acceptance draw for tests. Returns a value in [0, 1) compared to
    /// <c>Exp(scoreDifference / temperature)</c> for worsening moves.
    /// </summary>
    internal Func<double> NextAcceptanceDraw { get; set; } = static () => Random.Shared.NextDouble();

    /// <summary>
    /// Polish-only constructor for refining existing compositions without an inner producer.
    /// </summary>
    public SimulatedAnnealingWrapper(
        GroupCompositionScorer scorer,
        SimulatedAnnealingConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(scorer);

        this.inner = null;
        this.scorer = scorer;
        this.config = config ?? SimulatedAnnealingConfig.Default;
        knownStudentCount = null;
        ProposeSwap = ProposeRandomSwap;
    }

    public SimulatedAnnealingWrapper(
        StudentList studentList,
        GroupSizeDistribution groupSizes,
        GroupCompositionScorer scorer,
        SimulatedAnnealingConfig? config = null)
        : this(new MutualPairFirstStrategy(studentList, groupSizes), scorer, config, studentList.Students.Count)
    {
    }

    public SimulatedAnnealingWrapper(
        IGroupCompositionProducer inner,
        GroupCompositionScorer scorer,
        SimulatedAnnealingConfig? config = null)
        : this(inner, scorer, config, knownStudentCount: null)
    {
    }

    private SimulatedAnnealingWrapper(
        IGroupCompositionProducer inner,
        GroupCompositionScorer scorer,
        SimulatedAnnealingConfig? config,
        int? knownStudentCount)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(scorer);

        this.inner = inner;
        this.scorer = scorer;
        this.config = config ?? SimulatedAnnealingConfig.Default;
        this.knownStudentCount = knownStudentCount;
        ProposeSwap = ProposeRandomSwap;
    }

    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
    {
        if (inner is null)
        {
            throw new InvalidOperationException(
                "GenerateStream requires an inner producer. Use the constructor that accepts IGroupCompositionProducer, or call Polish directly.");
        }

        foreach (GroupComposition baseline in inner.GenerateStream(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return Polish(baseline, cancellationToken) with { TotalScore = 0 };
        }
    }

    /// <summary>
    /// Runs simulated annealing on a copy of <paramref name="baseline"/>.
    /// Does not mutate the input. Returns a new composition with <see cref="GroupComposition.TotalScore"/> set
    /// to the best score found during the search.
    /// </summary>
    public GroupComposition Polish(
        GroupComposition baseline,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        List<List<Student>> working = CloneMembers(baseline);
        double[] groupScores = working
            .Select(members => scorer.ScoreGroup(new Group(members)))
            .ToArray();
        double currentScore = groupScores.Sum();

        List<List<Student>> bestWorking = CloneMembers(working);
        double bestScore = currentScore;

        int studentCount = knownStudentCount
            ?? working.Sum(group => group.Count);
        int stepsPerTemp = config.ResolveStepsPerTemp(studentCount);
        double temperature = config.InitialTemperature;

        while (temperature > config.MinTemperature)
        {
            for (int step = 0; step < stepsPerTemp; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                double newA = scorer.ScoreGroup(new Group(working[groupA]));
                double newB = scorer.ScoreGroup(new Group(working[groupB]));
                double candidateScore = currentScore - groupScores[groupA] - groupScores[groupB] + newA + newB;
                double scoreDifference = candidateScore - currentScore;

                bool accept = scoreDifference > 0
                    || NextAcceptanceDraw() < Math.Exp(scoreDifference / temperature);

                if (accept)
                {
                    currentScore = candidateScore;
                    groupScores[groupA] = newA;
                    groupScores[groupB] = newB;

                    if (currentScore > bestScore)
                    {
                        bestScore = currentScore;
                        bestWorking = CloneMembers(working);
                    }
                }
                else
                {
                    working[groupA][indexA] = studentA;
                    working[groupB][indexB] = studentB;
                }
            }

            temperature *= config.CoolingRate;
        }

        return ToComposition(bestWorking) with { TotalScore = bestScore };
    }

    private static GroupComposition ToComposition(IReadOnlyList<List<Student>> groups) =>
        new(groups.Select(members => new Group(members.ToList())).ToList());

    private static List<List<Student>> CloneMembers(GroupComposition composition) =>
        composition.Groups.Select(group => group.Members.ToList()).ToList();

    private static List<List<Student>> CloneMembers(IReadOnlyList<List<Student>> groups) =>
        groups.Select(members => members.ToList()).ToList();

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
