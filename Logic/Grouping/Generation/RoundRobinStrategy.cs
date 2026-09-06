using Logic.Grouping.Scoring;
using Logic.Models;

namespace Logic.Grouping.Generation;

public class RoundRobinStrategy : IGroupCompositionProducer
{
    private readonly IReadOnlyList<IGroupCompositionProducer> producers;
    private readonly int phaseLength;

    public RoundRobinStrategy(
        StudentList studentList,
        GroupSizeDistribution groupSizes,
        GroupCompositionScorer scorer,
        int phaseLength = 500)
        : this(
            [
                new MutualPairFirstStrategy(studentList, groupSizes),
                new OrphanFirstStrategy(studentList, groupSizes),
                new TriadFirstStrategy(studentList, groupSizes),
                new MatrixWindowScanStrategy(studentList, groupSizes, scorer),
            ],
            phaseLength)
    {
    }

    public RoundRobinStrategy(
        IReadOnlyList<IGroupCompositionProducer> producers,
        int phaseLength = 500)
    {
        ArgumentNullException.ThrowIfNull(producers);

        if (producers.Count == 0)
        {
            throw new ArgumentException("At least one producer is required.", nameof(producers));
        }

        if (phaseLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(phaseLength), phaseLength, "Phase length must be at least 1.");
        }

        this.producers = producers;
        this.phaseLength = phaseLength;
    }

    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (IGroupCompositionProducer producer in producers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                int produced = 0;
                foreach (GroupComposition composition in producer.GenerateStream(cancellationToken))
                {
                    yield return composition;
                    produced++;

                    if (produced >= phaseLength || cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
            }
        }
    }
}
