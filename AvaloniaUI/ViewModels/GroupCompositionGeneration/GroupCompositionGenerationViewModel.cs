using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvaloniaUI.Data;
using AvaloniaUI.Data.InvalidationConfiguration;
using AvaloniaUI.Data.ScoringConfiguration;
using AvaloniaUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Logic.Grouping;
using Logic.Grouping.Generation;
using Logic.Grouping.Invalidation;
using Logic.Grouping.Scoring;
using Logic.Models;

namespace AvaloniaUI.ViewModels.GroupCompositionGeneration;

public partial class GroupCompositionGenerationViewModel : ViewModelBase
{
    private const int UiRefreshIntervalMs = 250;
    private const int YieldEveryIterations = 1000;

    private readonly InputConfiguration _inputConfiguration;
    private readonly IDialogService _dialogService;
    private readonly Action _navigateBack;
    private readonly GroupCompositionScorer _scorer;
    private readonly GroupCompositionInvalidator _invalidator;
    private readonly RandomShuffleStrategy _producer;
    private readonly TopCompositionKeeper _keeper = new();
    private readonly DispatcherTimer _uiRefreshTimer;

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _generationTask;
    private long _generatedCount;
    private long _duplicateRejectedCount;
    private long _lastRenderedRevision;

    public ObservableCollection<GroupCompositionCardViewModel> TopCompositions { get; } = [];

    [ObservableProperty]
    public partial bool IsGenerating { get; set; }

    [ObservableProperty]
    public partial long GeneratedCount { get; set; }

    [ObservableProperty]
    public partial long DuplicateRejectedCount { get; set; }

    [ObservableProperty]
    public partial string LastTopCompositionInsertedText { get; set; } = string.Empty;

    public string GenerationButtonText => IsGenerating ? "Stop" : "Start";

    public GroupCompositionGenerationViewModel() : this(
        new InputConfiguration
        {
            StudentList = StudentList.Create(
            [
                Student.Create("100001", Array.Empty<string>()),
                Student.Create("100002", Array.Empty<string>()),
                Student.Create("100003", Array.Empty<string>())
            ]),
            GroupSizeDistribution = GroupSizeDistribution.Create([2, 1], 3),
            EnabledScorers = [MutualMatchScoringConfiguration.Create(1)],
            EnabledInvalidators = []
        },
        new NullDialogService(),
        static () => { })
    {
    }

    public GroupCompositionGenerationViewModel(
        InputConfiguration inputConfiguration,
        IDialogService dialogService,
        Action navigateBack)
    {
        _inputConfiguration = inputConfiguration;
        _dialogService = dialogService;
        _navigateBack = navigateBack;

        var studentList = inputConfiguration.StudentList
            ?? throw new InvalidOperationException("Student list is required before generating group compositions.");
        var groupSizeDistribution = inputConfiguration.GroupSizeDistribution
            ?? throw new InvalidOperationException("Group size distribution is required before generating group compositions.");
        if (inputConfiguration.EnabledScorers.Count == 0)
        {
            throw new InvalidOperationException("At least one scoring rule is required before generating group compositions.");
        }

        _scorer = ScorerConfigurationMapper.ToScorer(inputConfiguration.EnabledScorers);
        _invalidator = InvalidatorConfigurationMapper.ToInvalidator(inputConfiguration.EnabledInvalidators);
        _producer = new RandomShuffleStrategy(studentList, groupSizeDistribution);

        _uiRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(UiRefreshIntervalMs)
        };
        _uiRefreshTimer.Tick += OnUiRefreshTimerTick;
    }

    partial void OnIsGeneratingChanged(bool value) => OnPropertyChanged(nameof(GenerationButtonText));

    [RelayCommand]
    private async Task ToggleGenerationAsync()
    {
        if (IsGenerating)
        {
            await StopGenerationAsync();
            return;
        }

        StartGeneration();
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        var wasGenerating = IsGenerating;
        if (wasGenerating)
        {
            await StopGenerationAsync();
        }

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Stop generation and discard results?");
        if (!confirmed)
        {
            if (wasGenerating)
            {
                StartGeneration();
            }

            return;
        }

        ClearGenerationState();
        _navigateBack();
    }

    private void StartGeneration()
    {
        if (IsGenerating)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        IsGenerating = true;
        _uiRefreshTimer.Start();

        _generationTask = Task.Run(async () =>
        {
            try
            {
                await GenerationLoopAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(
                    () => IsGenerating = false,
                    DispatcherPriority.Background);
            }
        }, cancellationToken);
    }

    private async Task StopGenerationAsync()
    {
        if (_cancellationTokenSource is null)
        {
            return;
        }

        await _cancellationTokenSource.CancelAsync();

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

        StopUiRefreshTimer();
        FlushUi();

        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
        _generationTask = null;
        IsGenerating = false;
    }

    private async Task GenerationLoopAsync(CancellationToken cancellationToken)
    {
        foreach (var rawComposition in _producer.GenerateStream(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var count = Interlocked.Increment(ref _generatedCount);
            if (count % YieldEveryIterations == 0)
            {
                await Task.Delay(1, cancellationToken);
            }
            
            if (_invalidator.ShouldReject(rawComposition))
            {
                  continue;
            }

            var scoredComposition = _scorer.Score(rawComposition);
            if (_keeper.TryAdd(scoredComposition) == TryAddResult.RejectedAsDuplicate)
            {
                Interlocked.Increment(ref _duplicateRejectedCount);
            }
        }
    }

    private void OnUiRefreshTimerTick(object? sender, EventArgs e) =>
        Dispatcher.UIThread.InvokeAsync(FlushUi, DispatcherPriority.Background);

    private void FlushUi()
    {
        GeneratedCount = _generatedCount;
        DuplicateRejectedCount = _duplicateRejectedCount;

        if (_keeper.Revision == _lastRenderedRevision)
        {
            return;
        }

        LastTopCompositionInsertedText = _keeper.LastInsertedAt is { } insertedAt
            ? insertedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)
            : string.Empty;
        RebuildTopCompositions();
        _lastRenderedRevision = _keeper.Revision;
    }

    private void RebuildTopCompositions()
    {
        TopCompositions.Clear();

        foreach (var composition in _keeper.Compositions)
        {
            TopCompositions.Add(GroupCompositionCardViewModel.FromModel(composition));
        }
    }

    private void ClearGenerationState()
    {
        StopUiRefreshTimer();
        _keeper.Clear();
        Interlocked.Exchange(ref _generatedCount, 0);
        Interlocked.Exchange(ref _duplicateRejectedCount, 0);
        _lastRenderedRevision = 0;
        GeneratedCount = 0;
        DuplicateRejectedCount = 0;
        LastTopCompositionInsertedText = string.Empty;
        TopCompositions.Clear();
    }

    private void StopUiRefreshTimer()
    {
        if (_uiRefreshTimer.IsEnabled)
        {
            _uiRefreshTimer.Stop();
        }
    }
}
