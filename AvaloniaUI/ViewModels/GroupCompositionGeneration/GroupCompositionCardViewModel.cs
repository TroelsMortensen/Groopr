using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Logic.Models;

namespace AvaloniaUI.ViewModels.GroupCompositionGeneration;

public partial class GroupCompositionCardViewModel
{
    public string ScoreText { get; }

    public string PlainText { get; }

    public IReadOnlyList<GroupCardViewModel> Groups { get; }

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
