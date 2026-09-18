using System.Diagnostics;
using System.Globalization;
using BlazorUI.Data;
using BlazorUI.Data.InvalidationConfiguration;
using BlazorUI.Data.ScoringConfiguration;
using BlazorUI.Models;
using BlazorUI.Services;
using Logic.Grouping;
using Logic.Grouping.Generation;
using Logic.Grouping.Invalidation;
using Logic.Grouping.Scoring;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages;

public partial class Generation : IDisposable
{
    // WASM is single-threaded: the browser only paints / handles clicks when we await.
    // Yield often enough that counters and Stop stay responsive, but not so often that
    // generation throughput collapses.
    private const int YieldIntervalMs = 200;
    private const int PolishIterations = 15;

    [Inject] private InputConfiguration InputConfiguration { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IClipboardService ClipboardService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private GroupCompositionScorer _scorer = null!;
    private GroupCompositionInvalidator _invalidator = null!;
    private IGroupCompositionProducer _producer = null!;
    private HillClimbingWrapper _climber = null!;
    private readonly TopCompositionKeeper _keeper = new();

    private CancellationTokenSource? _generationCts;
    private Task? _generationTask;
    private CancellationTokenSource? _polishCts;
    private Task? _polishTask;
    private long _generatedCount;
    private long _polishedCount;
    private long _duplicateRejectedCount;
    private long _lastRenderedRevision;
    private bool _ready;

    private List<CompositionCardModel> TopCompositions { get; } = [];
    private List<string> RecentTopCompositionInsertedTexts { get; } = [];

    private bool IsGenerating { get; set; }
    private bool IsPolishing { get; set; }
    private long GeneratedCount { get; set; }
    private long PolishedCount { get; set; }
    private long DuplicateRejectedCount { get; set; }
    private bool HasRecentInsertions { get; set; }
    private bool HasTopCompositions { get; set; }

    private string GenerationButtonText => IsGenerating ? "Stop" : "Start";
    private string PolishButtonText => IsPolishing ? "Stop polishing" : "Polish";
    private bool CanToggleGeneration => !IsPolishing;
    private bool CanTogglePolish => IsPolishing || (!IsGenerating && HasTopCompositions);

    protected override void OnInitialized()
    {
        if (InputConfiguration.StudentList is null
            || InputConfiguration.GroupSizeDistribution is null
            || InputConfiguration.EnabledScorers.Count == 0)
        {
            Navigation.NavigateTo("students", replace: true);
            return;
        }

        var studentList = InputConfiguration.StudentList;
        var groupSizeDistribution = InputConfiguration.GroupSizeDistribution;

        _scorer = ScorerConfigurationMapper.ToScorer(InputConfiguration.EnabledScorers);
        _invalidator = InvalidatorConfigurationMapper.ToInvalidator(InputConfiguration.EnabledInvalidators);
        _producer = new RoundRobinStrategy(studentList, groupSizeDistribution, _scorer);
        _climber = new HillClimbingWrapper(_scorer, iterations: PolishIterations);
        _ready = true;
    }

    private async Task ToggleGenerationAsync()
    {
        if (!_ready)
        {
            return;
        }

        if (IsGenerating)
        {
            await StopGenerationAsync();
            return;
        }

        await StartGenerationAsync();
    }

    private async Task TogglePolishAsync()
    {
        if (!_ready)
        {
            return;
        }

        if (IsPolishing)
        {
            await StopPolishingAsync();
            return;
        }

        await StartPolishingAsync();
    }

    private async Task CopyCompositionAsync(CompositionCardModel composition)
    {
        await ClipboardService.SetTextAsync(composition.PlainText);
        _ = composition.ShowCopiedFeedbackAsync();
    }

    private async Task BackAsync()
    {
        var wasGenerating = IsGenerating;
        var wasPolishing = IsPolishing;
        if (wasGenerating)
        {
            await StopGenerationAsync();
        }

        if (wasPolishing)
        {
            await StopPolishingAsync();
        }

        var confirmed = await DialogService.ShowConfirmAsync("Stop generation and discard results?");
        if (!confirmed)
        {
            if (wasGenerating)
            {
                await StartGenerationAsync();
            }

            if (wasPolishing)
            {
                await StartPolishingAsync();
            }

            return;
        }

        ClearGenerationState();
        Navigation.NavigateTo("invalidation-setup");
    }

    private async Task StartGenerationAsync()
    {
        if (IsGenerating || IsPolishing)
        {
            return;
        }

        _generationCts = new CancellationTokenSource();
        var cancellationToken = _generationCts.Token;
        IsGenerating = true;
        await InvokeAsync(StateHasChanged);

        _generationTask = GenerationLoopAsync(cancellationToken);
    }

    private async Task StopGenerationAsync()
    {
        if (_generationCts is null)
        {
            return;
        }

        await _generationCts.CancelAsync();

        if (_generationTask is not null)
        {
            try
            {
                await _generationTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        FlushUi();

        _generationCts.Dispose();
        _generationCts = null;
        _generationTask = null;
        IsGenerating = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task StartPolishingAsync()
    {
        if (IsPolishing || IsGenerating || _keeper.Compositions.Count == 0)
        {
            return;
        }

        _polishCts = new CancellationTokenSource();
        var cancellationToken = _polishCts.Token;
        IsPolishing = true;
        await InvokeAsync(StateHasChanged);

        _polishTask = PolishLoopAsync(cancellationToken);
    }

    private async Task StopPolishingAsync()
    {
        if (_polishCts is null)
        {
            return;
        }

        await _polishCts.CancelAsync();

        if (_polishTask is not null)
        {
            try
            {
                await _polishTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        FlushUi();

        _polishCts.Dispose();
        _polishCts = null;
        _polishTask = null;
        IsPolishing = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task GenerationLoopAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastYieldMs = 0L;

        try
        {
            foreach (var rawComposition in _producer.GenerateStream(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                Interlocked.Increment(ref _generatedCount);

                if (!_invalidator.ShouldReject(rawComposition))
                {
                    var scoredComposition = _scorer.Score(rawComposition);
                    if (_keeper.TryAdd(scoredComposition) == TryAddResult.RejectedAsDuplicate)
                    {
                        Interlocked.Increment(ref _duplicateRejectedCount);
                    }
                }

                if (stopwatch.ElapsedMilliseconds - lastYieldMs >= YieldIntervalMs)
                {
                    await YieldToUiAsync(cancellationToken);
                    lastYieldMs = stopwatch.ElapsedMilliseconds;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            FlushUi();
            IsGenerating = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task PolishLoopAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastYieldMs = 0L;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var snapshot = _keeper.Compositions.ToArray();
                if (snapshot.Length == 0)
                {
                    break;
                }

                foreach (var composition in snapshot)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var polished = _climber.Polish(composition, cancellationToken);
                    Interlocked.Increment(ref _polishedCount);
                    if (polished.TotalScore > composition.TotalScore
                        && !_invalidator.ShouldReject(polished))
                    {
                        _keeper.TryReplace(composition, polished);
                    }

                    if (stopwatch.ElapsedMilliseconds - lastYieldMs >= YieldIntervalMs)
                    {
                        await YieldToUiAsync(cancellationToken);
                        lastYieldMs = stopwatch.ElapsedMilliseconds;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            FlushUi();
            IsPolishing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task YieldToUiAsync(CancellationToken cancellationToken)
    {
        FlushUi();
        await InvokeAsync(StateHasChanged);
        // Task.Delay returns control to the browser event loop so paint/input can run.
        await Task.Delay(1, cancellationToken);
    }

    private void FlushUi()
    {
        // Counters always update so Start/Stop feedback stays live.
        GeneratedCount = _generatedCount;
        PolishedCount = _polishedCount;
        DuplicateRejectedCount = _duplicateRejectedCount;

        // Rebuild cards / recent insertions only when the top list actually changed
        // (TryAdd accepted or TryReplace improved a slot).
        if (_keeper.Revision == _lastRenderedRevision)
        {
            return;
        }

        RebuildRecentInsertions();
        RebuildTopCompositions();
        _lastRenderedRevision = _keeper.Revision;
    }

    private void RebuildRecentInsertions()
    {
        RecentTopCompositionInsertedTexts.Clear();
        foreach (var insertedAt in _keeper.RecentInsertedAt)
        {
            RecentTopCompositionInsertedTexts.Add(
                insertedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture));
        }

        HasRecentInsertions = RecentTopCompositionInsertedTexts.Count > 0;
    }

    private void RebuildTopCompositions()
    {
        foreach (var existing in TopCompositions)
        {
            existing.Changed -= OnCompositionCardChanged;
        }

        TopCompositions.Clear();

        foreach (var composition in _keeper.Compositions)
        {
            var card = CompositionCardModel.FromModel(composition);
            card.Changed += OnCompositionCardChanged;
            TopCompositions.Add(card);
        }

        HasTopCompositions = TopCompositions.Count > 0;
    }

    private void OnCompositionCardChanged() => InvokeAsync(StateHasChanged);

    private void ClearGenerationState()
    {
        _keeper.Clear();
        Interlocked.Exchange(ref _generatedCount, 0);
        Interlocked.Exchange(ref _polishedCount, 0);
        Interlocked.Exchange(ref _duplicateRejectedCount, 0);
        _lastRenderedRevision = 0;
        GeneratedCount = 0;
        PolishedCount = 0;
        DuplicateRejectedCount = 0;
        RecentTopCompositionInsertedTexts.Clear();
        HasRecentInsertions = false;
        HasTopCompositions = false;

        foreach (var existing in TopCompositions)
        {
            existing.Changed -= OnCompositionCardChanged;
        }

        TopCompositions.Clear();
    }

    public void Dispose()
    {
        _generationCts?.Cancel();
        _polishCts?.Cancel();
        _generationCts?.Dispose();
        _polishCts?.Dispose();

        foreach (var existing in TopCompositions)
        {
            existing.Changed -= OnCompositionCardChanged;
        }
    }
}
