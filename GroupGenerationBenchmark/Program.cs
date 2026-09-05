using System.Diagnostics;
using System.Globalization;
using GroupGenerationBenchmark;
using Logic.Grouping.Generation;
using Logic.Grouping.Scoring;
using Logic.Grouping.Scoring.ScoringStrategies;
using Logic.GroupSizing;
using Logic.Models;

const int Generations = 100_000;
const int WarmupGenerations = 1_000;

// Compact P10/P50/P90 + sparkline summary (in addition to full histograms). Set false to hide.
const bool ShowCompactDistributionSummary = false;

var students = BenchmarkStudentData.CreateStudents30();
var fixtureName = "Students30";
// var students = BenchmarkStudentData.CreateStudents50();
// var fixtureName = "Students50";

var sizes = GroupSizesCalculator.DetermineGroupSizes(
    students.Students.Count,
    GroupSizePriorities.Create([4, 3]));

var scorer = new GroupCompositionScorer(
[
    new MutualMatch(3),
    new PartialMatch(1),
    new NegativeMatch(3),
]);

var strategies = new List<(string Name, IGroupCompositionProducer Producer)>
{
    ("RandomShuffle", new RandomShuffleStrategy(students, sizes)),
    ("BreadthFirstGreedy", new BreadthFirstGreedyStrategy(students, sizes)),
    ("DepthFirstGreedy", new DepthFirstGreedyStrategy(students, sizes)),
    ("MutualPairFirst", new MutualPairFirstStrategy(students, sizes)),
    ("OrphanFirst", new OrphanFirstStrategy(students, sizes)),
    ("TriadFirst", new TriadFirstStrategy(students, sizes)),
    ("IslandFirst", new IslandFirstStrategy(students, sizes)),
    ("HillClimbing", new HillClimbingWrapper(students, sizes, scorer)),
    ("RoundRobin", new RoundRobinStrategy(students, sizes)),
};

PrintHeader(fixtureName, students, sizes, Generations);

Console.WriteLine("=== Generation speed ===");
Console.WriteLine($"{"Strategy",-22} {"Elapsed",12} {"Rate (comp/s)",16}");
Console.WriteLine(new string('-', 52));

foreach (var (name, producer) in strategies)
{
    var elapsed = TimeManyGroupGenerations(producer, Generations);
    double rate = Generations / elapsed.TotalSeconds;
    Console.WriteLine($"{name,-22} {FormatElapsed(elapsed),12} {rate,16:N0}");
}

Console.WriteLine();
Console.WriteLine("=== Score quality ===");
Console.WriteLine(
    $"{"Strategy",-22} {"Max",8} {"Mean",10} {"Median",10} {"StdDev",10} {"Min",8}");
Console.WriteLine(new string('-', 72));

var qualityByStrategy = new List<(string Name, QualityResult Quality)>();
foreach (var (name, producer) in strategies)
{
    var quality = MeasureGroupCompositionQuality(producer, scorer, Generations);
    qualityByStrategy.Add((name, quality));
    Console.WriteLine(
        $"{name,-22} {quality.Max,8:0.##} {quality.Mean,10:0.##} {quality.Median,10:0.##} {quality.StdDev,10:0.##} {quality.Min,8:0.##}");
}

// if (ShowCompactDistributionSummary)
// {
//     Console.WriteLine();
//     Console.WriteLine("=== Compact distribution (P10 / P50 / P90 + sparkline) ===");
//     Console.WriteLine(
//         $"{"Strategy",-22} {"P10",8} {"P50",8} {"P90",8}  Distribution");
//     Console.WriteLine(new string('-', 72));

//     double globalMin = qualityByStrategy.Min(q => q.Quality.Min);
//     double globalMax = qualityByStrategy.Max(q => q.Quality.Max);

//     foreach (var (name, quality) in qualityByStrategy)
//     {
//         double p10 = Percentile(quality.SortedScores, 0.10);
//         double p50 = Percentile(quality.SortedScores, 0.50);
//         double p90 = Percentile(quality.SortedScores, 0.90);
//         string sparkline = BuildSparkline(quality.SortedScores, globalMin, globalMax);
//         Console.WriteLine($"{name,-22} {p10,8:0.##} {p50,8:0.##} {p90,8:0.##}  {sparkline}");
//     }
// }

Console.WriteLine();
Console.WriteLine("=== Score histograms ===");
foreach (var (name, quality) in qualityByStrategy)
{
    Console.WriteLine();
    Console.WriteLine($"{name} (n={Generations:N0})");
    PrintHistogram(quality.Histogram);
}

static TimeSpan TimeManyGroupGenerations(IGroupCompositionProducer producer, int numberOfGenerations)
{
    foreach (var _ in producer.GenerateStream().Take(WarmupGenerations))
    {
    }

    var stopwatch = Stopwatch.StartNew();
    foreach (var _ in producer.GenerateStream().Take(numberOfGenerations))
    {
    }

    stopwatch.Stop();
    return stopwatch.Elapsed;
}

static QualityResult MeasureGroupCompositionQuality(
    IGroupCompositionProducer producer,
    GroupCompositionScorer scorer,
    int numberOfGenerations)
{
    var scores = new double[numberOfGenerations];
    var histogram = new Dictionary<double, int>();
    int index = 0;

    foreach (var composition in producer.GenerateStream().Take(numberOfGenerations))
    {
        double score = scorer.Score(composition).TotalScore;
        scores[index++] = score;

        if (histogram.TryGetValue(score, out int count))
            histogram[score] = count + 1;
        else
            histogram[score] = 1;
    }

    Array.Sort(scores);
    double mean = scores.Sum() / scores.Length;
    double median = Percentile(scores, 0.50);
    double variance = scores.Sum(s => (s - mean) * (s - mean)) / scores.Length;
    double stdDev = Math.Sqrt(variance);

    return new QualityResult(scores[^1], mean, median, stdDev, scores[0], histogram, scores);
}

static double Percentile(double[] sorted, double p)
{
    if (sorted.Length == 0)
        return 0;
    if (sorted.Length == 1)
        return sorted[0];

    double index = p * (sorted.Length - 1);
    int lo = (int)Math.Floor(index);
    int hi = (int)Math.Ceiling(index);
    if (lo == hi)
        return sorted[lo];
    double t = index - lo;
    return sorted[lo] * (1 - t) + sorted[hi] * t;
}

static string BuildSparkline(double[] sortedScores, double globalMin, double globalMax, int bins = 24)
{
    char[] blocks = ['▁', '▂', '▃', '▄', '▅', '▆', '▇', '█'];
    var counts = new int[bins];

    if (globalMax <= globalMin)
        return new string(blocks[0], bins);

    double width = globalMax - globalMin;
    foreach (double score in sortedScores)
    {
        int bin = (int)((score - globalMin) / width * bins);
        if (bin >= bins)
            bin = bins - 1;
        if (bin < 0)
            bin = 0;
        counts[bin]++;
    }

    int maxCount = counts.Max();
    if (maxCount == 0)
        return new string(blocks[0], bins);

    return string.Create(bins, (counts, maxCount), (span, state) =>
    {
        char[] levels = ['▁', '▂', '▃', '▄', '▅', '▆', '▇', '█'];
        for (int i = 0; i < span.Length; i++)
        {
            int level = (int)Math.Round(state.counts[i] * (levels.Length - 1) / (double)state.maxCount);
            span[i] = levels[level];
        }
    });
}

static void PrintHeader(string fixtureName, StudentList students, GroupSizeDistribution sizes, int generations)
{
    Console.WriteLine("Group composition generation benchmark");
    Console.WriteLine($"Fixture:     {fixtureName} ({students.Students.Count} students)");
    Console.WriteLine($"Blueprint:   [{string.Join(", ", sizes.Sizes)}]");
    Console.WriteLine("Scorers:     MutualMatch=3, PartialMatch=1, NegativeMatch=3");
    Console.WriteLine($"Generations: {generations:N0}");
    Console.WriteLine();
}

static void PrintHistogram(Dictionary<double, int> histogram)
{
    int maxCount = histogram.Values.Max();
    const int maxBar = 40;

    foreach (var (score, count) in histogram.OrderBy(kv => kv.Key))
    {
        int barLength = maxCount == 0 ? 0 : (int)Math.Round(count * (double)maxBar / maxCount);
        string bar = new string('#', Math.Max(barLength, count > 0 ? 1 : 0));
        Console.WriteLine($"  {score,8:0.##} : {count,7:N0}  {bar}");
    }
}

static string FormatElapsed(TimeSpan elapsed) =>
    elapsed.TotalSeconds >= 1
        ? elapsed.TotalSeconds.ToString("0.000s", CultureInfo.InvariantCulture)
        : elapsed.TotalMilliseconds.ToString("0.0ms", CultureInfo.InvariantCulture);

readonly record struct QualityResult(
    double Max,
    double Mean,
    double Median,
    double StdDev,
    double Min,
    Dictionary<double, int> Histogram,
    double[] SortedScores);
