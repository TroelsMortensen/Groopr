using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
    private const int PolishIterations = 15;

    private readonly InputConfiguration _inputConfiguration;
    private readonly IDialogService _dialogService;
    private readonly Action _navigateBack;
    private readonly GroupCompositionScorer _scorer;
    private readonly GroupCompositionInvalidator _invalidator;
    private readonly IGroupCompositionProducer _producer;
    private readonly HillClimbingWrapper _climber;
    private readonly TopCompositionKeeper _keeper = new();
    private readonly DispatcherTimer _uiRefreshTimer;

    private CancellationTokenSource? _generationCts;
    private Task? _generationTask;
    private CancellationTokenSource? _polishCts;
    private Task? _polishTask;
    private long _generatedCount;
    private long _polishedCount;
    private long _duplicateRejectedCount;
    private long _lastRenderedRevision;

    public ObservableCollection<GroupCompositionCardViewModel> TopCompositions { get; } = [];

    public ObservableCollection<string> RecentTopCompositionInsertedTexts { get; } = [];

    [ObservableProperty]
    public partial bool IsGenerating { get; set; }

    [ObservableProperty]
    public partial bool IsPolishing { get; set; }

    [ObservableProperty]
    public partial long GeneratedCount { get; set; }

    [ObservableProperty]
    public partial long PolishedCount { get; set; }

    [ObservableProperty]
    public partial long DuplicateRejectedCount { get; set; }

    [ObservableProperty]
    public partial bool HasRecentInsertions { get; set; }

    [ObservableProperty]
    public partial bool HasTopCompositions { get; set; }

    public string GenerationButtonText => IsGenerating ? "Stop" : "Start";

    public string PolishButtonText => IsPolishing ? "Stop polishing" : "Polish";

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
        _producer = new RoundRobinStrategy(studentList, groupSizeDistribution, _scorer);
        _climber = new HillClimbingWrapper(_scorer, iterations: PolishIterations);

        _uiRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(UiRefreshIntervalMs)
        };
        _uiRefreshTimer.Tick += OnUiRefreshTimerTick;
    }

    partial void OnIsGeneratingChanged(bool value)
    {
        OnPropertyChanged(nameof(GenerationButtonText));
        ToggleGenerationCommand.NotifyCanExecuteChanged();
        TogglePolishCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsPolishingChanged(bool value)
    {
        OnPropertyChanged(nameof(PolishButtonText));
        ToggleGenerationCommand.NotifyCanExecuteChanged();
        TogglePolishCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasTopCompositionsChanged(bool value) =>
        TogglePolishCommand.NotifyCanExecuteChanged();

    private bool CanToggleGeneration() => !IsPolishing;

    private bool CanTogglePolish() =>
        IsPolishing || (!IsGenerating && HasTopCompositions);

    [RelayCommand(CanExecute = nameof(CanToggleGeneration))]
    private async Task ToggleGenerationAsync()
    {
        if (IsGenerating)
        {
            await StopGenerationAsync();
            return;
        }

        StartGeneration();
    }

    [RelayCommand(CanExecute = nameof(CanTogglePolish))]
    private async Task TogglePolishAsync()
    {
        if (IsPolishing)
        {
            await StopPolishingAsync();
            return;
        }

        StartPolishing();
    }

    [RelayCommand]
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

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Stop generation and discard results?");
        if (!confirmed)
        {
            if (wasGenerating)
            {
                StartGeneration();
            }

            if (wasPolishing)
            {
                StartPolishing();
            }

            return;
        }

        ClearGenerationState();
        _navigateBack();
    }

    private void StartGeneration()
    {
        if (IsGenerating || IsPolishing)
        {
            return;
        }

        _generationCts = new CancellationTokenSource();
        var cancellationToken = _generationCts.Token;
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

        if (!IsPolishing)
        {
            StopUiRefreshTimer();
            FlushUi();
        }

        _generationCts.Dispose();
        _generationCts = null;
        _generationTask = null;
        IsGenerating = false;
    }

    private void StartPolishing()
    {
        if (IsPolishing || IsGenerating || _keeper.Compositions.Count == 0)
        {
            return;
        }

        _polishCts = new CancellationTokenSource();
        var cancellationToken = _polishCts.Token;
        IsPolishing = true;
        _uiRefreshTimer.Start();

        _polishTask = Task.Run(async () =>
        {
            try
            {
                await PolishLoopAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(
                    () => IsPolishing = false,
                    DispatcherPriority.Background);
            }
        }, cancellationToken);
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

        if (!IsGenerating)
        {
            StopUiRefreshTimer();
            FlushUi();
        }

        _polishCts.Dispose();
        _polishCts = null;
        _polishTask = null;
        IsPolishing = false;
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

    private async Task PolishLoopAsync(CancellationToken cancellationToken)
    {
        long polishAttempts = 0;
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

                polishAttempts++;
                if (polishAttempts % YieldEveryIterations == 0)
                {
                    await Task.Delay(1, cancellationToken);
                }

                var polished = _climber.Polish(composition, cancellationToken);
                Interlocked.Increment(ref _polishedCount);
                if (polished.TotalScore <= composition.TotalScore)
                {
                    continue;
                }

                if (_invalidator.ShouldReject(polished))
                {
                    continue;
                }

                _keeper.TryReplace(composition, polished);
            }

            // Polish is much slower than random generation; yield once per pass over the top list.
            await Task.Delay(1, cancellationToken);
        }
    }

    private void OnUiRefreshTimerTick(object? sender, EventArgs e) =>
        Dispatcher.UIThread.InvokeAsync(FlushUi, DispatcherPriority.Background);

    private void FlushUi()
    {
        GeneratedCount = _generatedCount;
        PolishedCount = _polishedCount;
        DuplicateRejectedCount = _duplicateRejectedCount;

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
        TopCompositions.Clear();

        foreach (var composition in _keeper.Compositions)
        {
            TopCompositions.Add(GroupCompositionCardViewModel.FromModel(composition));
        }

        HasTopCompositions = TopCompositions.Count > 0;
    }

    private void ClearGenerationState()
    {
        StopUiRefreshTimer();
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
