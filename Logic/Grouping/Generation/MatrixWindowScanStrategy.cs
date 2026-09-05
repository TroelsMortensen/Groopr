using Logic.Grouping.Scoring;
using Logic.Models;

namespace Logic.Grouping.Generation;

/// <summary>
/// Builds groups by anchoring on the highest-scoring available dyad, then greedily
/// expanding by marginal group score until the blueprint size is reached.
/// </summary>
public class MatrixWindowScanStrategy : IGroupCompositionProducer
{
    private readonly IReadOnlyList<Student> students;
    private readonly IReadOnlyList<int> groupSizes;
    private readonly GroupCompositionScorer scorer;

    /// <summary>
    /// Used for unit testing. Randomizes student order once per composition so tie-breaks vary.
    /// </summary>
    internal Func<IReadOnlyList<Student>, IReadOnlyList<Student>> Shuffle { get; set; } =
        students => students.OrderBy(_ => Random.Shared.Next()).ToList();

    public MatrixWindowScanStrategy(
        StudentList studentList,
        GroupSizeDistribution groupSizes,
        GroupCompositionScorer scorer)
    {
        ArgumentNullException.ThrowIfNull(scorer);

        if (studentList.Students.Count != groupSizes.Sizes.Sum())
        {
            throw new ArgumentException(
                $"The sum of group sizes must equal the number of students ({studentList.Students.Count}).");
        }

        students = studentList.Students;
        this.groupSizes = groupSizes.Sizes;
        this.scorer = scorer;
    }

    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var unassigned = Shuffle(students).ToList();
            var candidateDyads = BuildSortedDyads(unassigned);
            var groups = new List<Group>(groupSizes.Count);

            foreach (int targetSize in groupSizes)
            {
                groups.Add(BuildGroup(targetSize, unassigned, candidateDyads));
            }

            yield return new GroupComposition(groups, 0);
        }
    }

    private Group BuildGroup(
        int targetSize,
        List<Student> unassigned,
        List<(Student A, Student B)> candidateDyads)
    {
        List<Student> bestGroup;
        if (targetSize < 2)
        {
            bestGroup = unassigned.Take(targetSize).ToList();
        }
        else
        {
            (Student A, Student B)? anchor = FindAvailableAnchor(unassigned, candidateDyads);
            if (anchor is null)
            {
                bestGroup = unassigned.Take(targetSize).ToList();
            }
            else if (targetSize == 2)
            {
                bestGroup = [anchor.Value.A, anchor.Value.B];
            }
            else
            {
                bestGroup = ExpandFromAnchor(targetSize, unassigned, anchor.Value);
            }
        }

        foreach (Student member in bestGroup)
        {
            unassigned.Remove(member);
        }

        return new Group(bestGroup);
    }

    private List<Student> ExpandFromAnchor(
        int targetSize,
        List<Student> unassigned,
        (Student A, Student B) anchor)
    {
        var currentGroup = new List<Student>(targetSize) { anchor.A, anchor.B };
        var inGroup = new HashSet<Student> { anchor.A, anchor.B };

        while (currentGroup.Count < targetSize)
        {
            Student? bestCandidate = null;
            double bestMarginalScore = double.NegativeInfinity;
            double currentScore = scorer.ScoreGroup(new Group(currentGroup));

            foreach (Student student in unassigned)
            {
                if (inGroup.Contains(student) || !PassesHardConstraints(currentGroup, student))
                {
                    continue;
                }

                double marginalScore = EvaluateAddition(currentGroup, student, currentScore);
                if (marginalScore > bestMarginalScore)
                {
                    bestMarginalScore = marginalScore;
                    bestCandidate = student;
                }
            }

            if (bestCandidate is null)
            {
                bestCandidate = unassigned.First(student => !inGroup.Contains(student));
            }

            currentGroup.Add(bestCandidate);
            inGroup.Add(bestCandidate);
        }

        return currentGroup;
    }

    private double EvaluateAddition(List<Student> currentGroup, Student student, double currentScore)
    {
        var trial = new List<Student>(currentGroup.Count + 1);
        trial.AddRange(currentGroup);
        trial.Add(student);
        return scorer.ScoreGroup(new Group(trial)) - currentScore;
    }

    /// <summary>
    /// Hard constraints for mid-build membership. Currently none at generation time
    /// (composition-level hard rejects run after generation in the search loop).
    /// </summary>
    private static bool PassesHardConstraints(IReadOnlyList<Student> _, Student __) => true;

    private static (Student A, Student B)? FindAvailableAnchor(
        List<Student> unassigned,
        List<(Student A, Student B)> candidateDyads)
    {
        var unassignedSet = unassigned.ToHashSet();
        foreach ((Student a, Student b) in candidateDyads)
        {
            if (unassignedSet.Contains(a) && unassignedSet.Contains(b))
            {
                return (a, b);
            }
        }

        return null;
    }

    private List<(Student A, Student B)> BuildSortedDyads(IReadOnlyList<Student> pool)
    {
        var pairs = new List<(Student A, Student B, double Score)>();
        for (int i = 0; i < pool.Count; i++)
        {
            for (int j = i + 1; j < pool.Count; j++)
            {
                Student a = pool[i];
                Student b = pool[j];
                double score = scorer.ScoreGroup(new Group([a, b]));
                pairs.Add((a, b, score));
            }
        }

        return pairs
            .OrderByDescending(pair => pair.Score)
            .Select(pair => (pair.A, pair.B))
            .ToList();
    }
}
