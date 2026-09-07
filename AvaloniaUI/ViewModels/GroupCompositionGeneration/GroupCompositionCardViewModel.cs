using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Logic.Models;

namespace AvaloniaUI.ViewModels.GroupCompositionGeneration;

public partial class GroupCompositionCardViewModel : ObservableObject
{
    private const int CopiedFeedbackDurationMs = 1500;

    private CancellationTokenSource? _copiedFeedbackCts;

    public string ScoreText { get; }

    public string PlainText { get; }

    public IReadOnlyList<GroupCardViewModel> Groups { get; }

    [ObservableProperty]
    public partial bool ShowCopiedFeedback { get; set; }

    private GroupCompositionCardViewModel(
        string scoreText,
        string plainText,
        IReadOnlyList<GroupCardViewModel> groups)
    {
        ScoreText = scoreText;
        PlainText = plainText;
        Groups = groups;
    }

    public static GroupCompositionCardViewModel FromModel(GroupComposition composition)
    {
        var scoreText = composition.TotalScore.ToString("0.0", CultureInfo.InvariantCulture);
        var groups = composition.Groups
            .Select((group, index) => GroupCardViewModel.FromGroup(group, index + 1))
            .ToList();

        return new GroupCompositionCardViewModel(scoreText, BuildPlainText(scoreText, groups), groups);
    }

    public async Task ShowCopiedFeedbackAsync()
    {
        _copiedFeedbackCts?.Cancel();
        _copiedFeedbackCts?.Dispose();
        _copiedFeedbackCts = new CancellationTokenSource();
        var cancellationToken = _copiedFeedbackCts.Token;

        ShowCopiedFeedback = true;
        try
        {
            await Task.Delay(CopiedFeedbackDurationMs, cancellationToken);
            ShowCopiedFeedback = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string BuildPlainText(string scoreText, IReadOnlyList<GroupCardViewModel> groups)
    {
        var builder = new StringBuilder();
        builder.Append("Score: ").Append(scoreText);

        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            builder.AppendLine().AppendLine();
            builder.Append(group.Title);

            foreach (var member in group.Members)
            {
                builder.AppendLine();
                builder.Append("- ").Append(member.DisplayText);
            }
        }

        return builder.ToString();
    }
}
