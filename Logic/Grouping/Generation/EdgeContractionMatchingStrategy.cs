using Logic.Grouping.Scoring;
using Logic.Models;

namespace Logic.Grouping.Generation;

/// <summary>
/// Agglomerative clustering: start with singleton nodes, repeatedly contract the
/// highest-affinity edge that respects max group size, then rebalance to the
/// exact blueprint sizes.
/// </summary>
public class EdgeContractionMatchingStrategy : IGroupCompositionProducer
{
    private readonly IReadOnlyList<Student> students;
    private readonly IReadOnlyList<int> groupSizes;
    private readonly GroupCompositionScorer scorer;
    private readonly int maxGroupSize;
    private readonly int targetGroupCount;

    /// <summary>
    /// Used for unit testing. Randomizes student order once per composition so tie-breaks vary.
    /// </summary>
    internal Func<IReadOnlyList<Student>, IReadOnlyList<Student>> Shuffle { get; set; } =
        students => students.OrderBy(_ => Random.Shared.Next()).ToList();

    public EdgeContractionMatchingStrategy(
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
        maxGroupSize = groupSizes.Sizes.Max();
        targetGroupCount = groupSizes.Sizes.Count;
    }

    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IReadOnlyList<Student> ordered = Shuffle(students);
            yield return BuildComposition(ordered);
        }
    }

    private GroupComposition BuildComposition(IReadOnlyList<Student> orderedStudents)
    {
        int tieBreak = 0;
        var activeNodes = new HashSet<GroupNode>();
        foreach (Student student in orderedStudents)
        {
            activeNodes.Add(new GroupNode(student, tieBreak++));
        }

        var edgeQueue = new PriorityQueue<Edge, EdgePriority>();
        EnqueueAllInterNodeEdges(activeNodes, edgeQueue);

        while (activeNodes.Count > targetGroupCount && edgeQueue.Count > 0)
        {
            Edge bestEdge = edgeQueue.Dequeue();
            if (!IsEdgeValid(bestEdge, activeNodes))
            {
                continue;
            }

            GroupNode nodeA = bestEdge.Source;
            GroupNode nodeB = bestEdge.Target;

            if (nodeA.Size + nodeB.Size > GetMaxGroupSizeForNextSlot())
            {
                continue;
            }

            Contract(nodeA, nodeB, activeNodes, edgeQueue);
        }

        List<GroupNode> finalized = FinalizeGroupSizes(activeNodes.ToList());
        var groups = new List<Group>(groupSizes.Count);
        for (int i = 0; i < groupSizes.Count; i++)
        {
            groups.Add(new Group(finalized[i].Members));
        }

        return new GroupComposition(groups, 0);
    }

    private void Contract(
        GroupNode survivor,
        GroupNode absorbed,
        HashSet<GroupNode> activeNodes,
        PriorityQueue<Edge, EdgePriority> edgeQueue)
    {
        survivor.Merge(absorbed, ScoreNode);
        activeNodes.Remove(absorbed);
        UpdateConnectedEdges(survivor, activeNodes, edgeQueue);
    }

    private void UpdateConnectedEdges(
        GroupNode expanded,
        HashSet<GroupNode> activeNodes,
        PriorityQueue<Edge, EdgePriority> edgeQueue)
    {
        foreach (GroupNode other in activeNodes)
        {
            if (ReferenceEquals(other, expanded))
            {
                continue;
            }

            EnqueueEdge(expanded, other, edgeQueue);
        }
    }

    private void EnqueueAllInterNodeEdges(
        HashSet<GroupNode> activeNodes,
        PriorityQueue<Edge, EdgePriority> edgeQueue)
    {
        var nodeList = activeNodes.ToList();
        for (int i = 0; i < nodeList.Count; i++)
        {
            for (int j = i + 1; j < nodeList.Count; j++)
            {
                EnqueueEdge(nodeList[i], nodeList[j], edgeQueue);
            }
        }
    }

    private void EnqueueEdge(
        GroupNode a,
        GroupNode b,
        PriorityQueue<Edge, EdgePriority> edgeQueue)
    {
        double weight = ComputeEdgeWeight(a, b);
        var edge = new Edge(a, b, a.Generation, b.Generation);
        edgeQueue.Enqueue(edge, new EdgePriority(weight, a.TieBreak, b.TieBreak));
    }

    private double ComputeEdgeWeight(GroupNode a, GroupNode b)
    {
        var combined = new List<Student>(a.Size + b.Size);
        combined.AddRange(a.Members);
        combined.AddRange(b.Members);
        return ScoreMembers(combined) - a.CachedScore - b.CachedScore;
    }

    private double ScoreNode(GroupNode node) => ScoreMembers(node.Members);

    private double ScoreMembers(IReadOnlyList<Student> members) =>
        members.Count == 0 ? 0 : scorer.ScoreGroup(new Group(members));

    private int GetMaxGroupSizeForNextSlot() => maxGroupSize;

    private static bool IsEdgeValid(Edge edge, HashSet<GroupNode> activeNodes) =>
        activeNodes.Contains(edge.Source)
        && activeNodes.Contains(edge.Target)
        && edge.Source.Generation == edge.SourceGeneration
        && edge.Target.Generation == edge.TargetGeneration;

    /// <summary>
    /// Packs stray nodes down to the target count (forced merges), then rebalances
    /// member counts to match the blueprint sizes exactly.
    /// </summary>
    private List<GroupNode> FinalizeGroupSizes(List<GroupNode> activeNodes)
    {
        while (activeNodes.Count > targetGroupCount)
        {
            ForceMergeBestPair(activeNodes);
        }

        return RebalanceToBlueprint(activeNodes);
    }

    private void ForceMergeBestPair(List<GroupNode> activeNodes)
    {
        GroupNode? bestA = null;
        GroupNode? bestB = null;
        double bestWeight = double.NegativeInfinity;
        bool foundWithinCapacity = false;

        for (int i = 0; i < activeNodes.Count; i++)
        {
            for (int j = i + 1; j < activeNodes.Count; j++)
            {
                GroupNode a = activeNodes[i];
                GroupNode b = activeNodes[j];
                double weight = ComputeEdgeWeight(a, b);
                bool withinCapacity = a.Size + b.Size <= maxGroupSize;

                if (withinCapacity)
                {
                    if (!foundWithinCapacity || weight > bestWeight)
                    {
                        foundWithinCapacity = true;
                        bestWeight = weight;
                        bestA = a;
                        bestB = b;
                    }
                }
                else if (!foundWithinCapacity && weight > bestWeight)
                {
                    bestWeight = weight;
                    bestA = a;
                    bestB = b;
                }
            }
        }

        if (bestA is null || bestB is null)
        {
            throw new InvalidOperationException("Unable to force-merge remaining nodes.");
        }

        bestA.Merge(bestB, ScoreNode);
        activeNodes.Remove(bestB);
    }

    private List<GroupNode> RebalanceToBlueprint(List<GroupNode> nodes)
    {
        var remainingNodes = nodes.OrderByDescending(node => node.Size).ToList();
        var targetSlots = groupSizes
            .Select((size, index) => (Size: size, Index: index))
            .OrderByDescending(slot => slot.Size)
            .ToList();

        var assigned = new GroupNode[groupSizes.Count];
        for (int i = 0; i < remainingNodes.Count; i++)
        {
            assigned[targetSlots[i].Index] = remainingNodes[i];
        }

        var result = assigned.ToList();
        TransferToExactSizes(result);
        return result;
    }

    private void TransferToExactSizes(List<GroupNode> groups)
    {
        while (true)
        {
            int overIndex = -1;
            int underIndex = -1;
            for (int i = 0; i < groups.Count; i++)
            {
                if (overIndex < 0 && groups[i].Size > groupSizes[i])
                {
                    overIndex = i;
                }

                if (underIndex < 0 && groups[i].Size < groupSizes[i])
                {
                    underIndex = i;
                }
            }

            if (overIndex < 0 || underIndex < 0)
            {
                break;
            }

            MoveLeastCostlyStudent(groups[overIndex], groups[underIndex]);
        }
    }

    private void MoveLeastCostlyStudent(GroupNode from, GroupNode to)
    {
        Student? bestStudent = null;
        double bestDelta = double.NegativeInfinity;

        foreach (Student student in from.Members)
        {
            double delta = EvaluateMoveDelta(from, to, student);
            if (bestStudent is null || delta > bestDelta)
            {
                bestDelta = delta;
                bestStudent = student;
            }
        }

        if (bestStudent is null)
        {
            throw new InvalidOperationException("Oversized group has no students to move.");
        }

        from.Remove(bestStudent, ScoreNode);
        to.Add(bestStudent, ScoreNode);
    }

    private double EvaluateMoveDelta(GroupNode from, GroupNode to, Student student)
    {
        double scoreFromBefore = from.CachedScore;
        double scoreToBefore = to.CachedScore;

        var fromWithout = from.Members.Where(member => !ReferenceEquals(member, student)).ToList();
        var toWith = new List<Student>(to.Members) { student };

        return (ScoreMembers(fromWithout) + ScoreMembers(toWith)) - (scoreFromBefore + scoreToBefore);
    }

    private readonly struct Edge(
        GroupNode source,
        GroupNode target,
        int sourceGeneration,
        int targetGeneration)
    {
        public GroupNode Source { get; } = source;
        public GroupNode Target { get; } = target;
        public int SourceGeneration { get; } = sourceGeneration;
        public int TargetGeneration { get; } = targetGeneration;
    }

    /// <summary>
    /// Max-heap ordering via negated weight comparison; tie-break on shuffle order.
    /// </summary>
    private readonly struct EdgePriority(double weight, int tieBreakA, int tieBreakB)
        : IComparable<EdgePriority>
    {
        private double Weight { get; } = weight;
        private int TieBreakA { get; } = Math.Min(tieBreakA, tieBreakB);
        private int TieBreakB { get; } = Math.Max(tieBreakA, tieBreakB);

        public int CompareTo(EdgePriority other)
        {
            int weightComparison = other.Weight.CompareTo(Weight);
            if (weightComparison != 0)
            {
                return weightComparison;
            }

            int tieA = TieBreakA.CompareTo(other.TieBreakA);
            if (tieA != 0)
            {
                return tieA;
            }

            return TieBreakB.CompareTo(other.TieBreakB);
        }
    }

    private sealed class GroupNode(Student student, int tieBreak)
    {
        public List<Student> Members { get; } = [student];
        public int Size => Members.Count;
        public int Generation { get; private set; }
        public int TieBreak { get; } = tieBreak;
        public double CachedScore { get; private set; }

        public void Merge(GroupNode other, Func<GroupNode, double> score)
        {
            Members.AddRange(other.Members);
            Generation++;
            CachedScore = score(this);
        }

        public void Add(Student student, Func<GroupNode, double> score)
        {
            Members.Add(student);
            Generation++;
            CachedScore = score(this);
        }

        public void Remove(Student student, Func<GroupNode, double> score)
        {
            Members.Remove(student);
            Generation++;
            CachedScore = score(this);
        }
    }
}
