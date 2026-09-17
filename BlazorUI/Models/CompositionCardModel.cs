using System.Globalization;
using System.Text;
using Logic.Models;

namespace BlazorUI.Models;

public class CompositionCardModel
{
    private const int CopiedFeedbackDurationMs = 1500;

    private CancellationTokenSource? _copiedFeedbackCts;

    public string ScoreText { get; }
    public string PlainText { get; }
    public bool ShowCopiedFeedback { get; private set; }

    public event Action? Changed;

    private CompositionCardModel(string scoreText, string plainText)
    {
        ScoreText = scoreText;
        PlainText = plainText;
    }

    public static CompositionCardModel FromModel(GroupComposition composition)
    {
        var scoreText = composition.TotalScore.ToString("0.0", CultureInfo.InvariantCulture);
        var groups = composition.Groups
            .Select((group, index) => (Title: $"Group {index + 1}", Members: group.Members))
            .ToList();

        return new CompositionCardModel(scoreText, BuildPlainText(scoreText, groups));
    }

    public async Task ShowCopiedFeedbackAsync()
    {
        _copiedFeedbackCts?.Cancel();
        _copiedFeedbackCts?.Dispose();
        _copiedFeedbackCts = new CancellationTokenSource();
        var cancellationToken = _copiedFeedbackCts.Token;

        ShowCopiedFeedback = true;
        Changed?.Invoke();
        try
        {
            await Task.Delay(CopiedFeedbackDurationMs, cancellationToken);
            ShowCopiedFeedback = false;
            Changed?.Invoke();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string BuildPlainText(
        string scoreText,
        IReadOnlyList<(string Title, IReadOnlyList<Student> Members)> groups)
    {
        var builder = new StringBuilder();
        builder.Append("Score: ").Append(scoreText);

        foreach (var group in groups)
        {
            builder.AppendLine().AppendLine();
            builder.Append(group.Title);

            foreach (var member in group.Members)
            {
                var name = string.IsNullOrWhiteSpace(member.Name) ? "Unknown" : member.Name;
                builder.AppendLine();
                builder.Append("- ").Append(member.Number).Append(", ").Append(name);
            }
        }

        return builder.ToString();
    }
}
